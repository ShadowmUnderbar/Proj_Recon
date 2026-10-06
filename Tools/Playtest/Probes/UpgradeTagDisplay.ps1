#
# アップグレードのタグ表示（UpgradeTagTickerView / ShopView の所持タグ上位）を確かめるプローブ。
#
#   - TagText テーブルが読み込まれ、タグ名がキー（$Barrier など）ではなく表示名で出る
#   - 3Dカードの TagText に、候補のタグがマスターデータの並び順で入る（収まらないときは2周ぶん並べて流す）
#   - 収まらないタグは時間で左へ流れ、表示枠の外の文字は透明になる
#   - 所持タグ上位は、同じアップグレードのレベル違いを1件と数え、件数の多い順・同数は定義順で出る
#
# 開始時アップグレードが混ざると所持タグの期待値が決まらないため、BossProbeCommon の
# Enter-BossProbeScene で実行中だけ空にする（再生開始シーンも Battle にする）。
# カードはVRだと手の前に並ばず画面で確認しにくいため、ShopPurchase と同じく VR モードを一時的に切る。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

function ProbePrepare {
    Enter-BossProbeScene
    $Global:TagProbePreviousVrMode = Invoke-UnityCode -Snippet @'
var before = UnityEditor.EditorPrefs.GetBool("VRMode", false);
UnityEditor.EditorPrefs.SetBool("VRMode", false);
return before.ToString().ToLower();
'@
    Write-Host "VRモードを一時的に無効化しました（元の値: $Global:TagProbePreviousVrMode）"
}

function ProbeCleanup {
    Exit-BossProbeScene
    if ($null -eq $Global:TagProbePreviousVrMode) { return }

    Invoke-UnityCode -Snippet (@'
UnityEditor.EditorPrefs.SetBool("VRMode", __PREVIOUS__);
return "restored";
'@.Replace('__PREVIOUS__', $Global:TagProbePreviousVrMode)) | Out-Null
    Write-Host "VRモードを元に戻しました（$Global:TagProbePreviousVrMode）"
    $Global:TagProbePreviousVrMode = $null
}

function ProbeRun {
    # --- 1. 所持アップグレードを仕込んでからショップを開く ---
    # レイジ Lv1・Lv2（レイジ/被ダメ）、アドレナリン Lv1（被ダメ）、メデューサ Lv1（注視/スタン）
    $opened = Invoke-UnityJson -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.UseCase;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var database = scope.Container.Resolve<UpgradeDatabase>();
var ownedBefore = session.AppliedUpgrades.Count;

foreach (var assetName in new[] { "Rage_L1", "Rage_L2", "Adrenalin_L1", "Medusa_L1" })
{
    session.AddUpgrade(database.UpgradeMasterData.First(d => d.name == assetName));
}

var shop = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<ShopUseCase>().First();
typeof(ShopUseCase).GetMethod("OpenShopForDebug", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shop, null);

return $"{{\"ownedBefore\":{ownedBefore},\"ownedAfter\":{session.AppliedUpgrades.Count}}}";
'@
    Assert-ProbeValue -Name '仕込む前の所持アップグレード数（開始時アップグレードなし）' -Actual ([double]$opened.ownedBefore) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '仕込んだ所持アップグレード数' -Actual ([double]$opened.ownedAfter) -Expected 4 | Out-Null

    Start-Sleep -Milliseconds 800

    # --- 2. 所持タグ上位 ---
    $ranking = Invoke-UnityJson -Snippet @'
using UnityEngine;

var label = GameObject.Find("OwnedTagRankingText");
var text = label != null ? label.GetComponent<UnityEngine.UI.Text>().text : "(not found)";
var expected = "所持タグ\n被ダメ ×2\nレイジ ×1\nスタン ×1\n注視 ×1";
return $"{{\"matched\":{(text == expected).ToString().ToLower()},\"actual\":\"{text.Replace("\n", " | ")}\"}}";
'@
    Assert-ProbeTrue -Name '所持タグ上位がレベル違いを1件・件数順・同数は定義順で出る' -Condition ([bool]$ranking.matched) `
        -Detail "(実測: $($ranking.actual))" | Out-Null

    # --- 3. カードのタグ ---
    $cards = Invoke-UnityJson -Snippet @'
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Data.MasterData;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var tagNames = scope.Container.Resolve<ITagLocalizationDataStore>();
var shop = scope.Container.Resolve<System.Collections.Generic.IReadOnlyList<IInitializable>>().OfType<ShopUseCase>().First();
var candidates = (System.Collections.Generic.List<UpgradeMasterData>)typeof(ShopUseCase)
    .GetField("_currentCandidates", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shop);
const string separator = "  /  ";

var total = 0; var matched = 0; var tagged = 0; var scrolling = 0; var keyNames = 0;
var mismatches = new System.Collections.Generic.List<string>();
foreach (var card in UnityEngine.Object.FindObjectsOfType<UpgradeCardView>())
{
    total++;
    var upgrade = candidates[card.Index];
    var names = upgrade.Tags.Select(t => tagNames.GetName(t)).ToArray();
    keyNames += names.Count(n => n.StartsWith("$"));
    var joined = string.Join(separator, names);
    var cycle = joined + separator;
    var actual = card.transform.Find("TagText").GetComponent<TMPro.TextMeshPro>().text;
    if (names.Length > 0) tagged++;
    if (actual == cycle + cycle) scrolling++;
    if (actual == joined || (names.Length > 0 && actual == cycle + cycle)) matched++;
    else mismatches.Add($"{upgrade.name}: [{actual}] vs [{joined}]");
}

return $"{{\"total\":{total},\"matched\":{matched},\"tagged\":{tagged},\"scrolling\":{scrolling},\"keyNames\":{keyNames},\"mismatches\":\"{string.Join(" ; ", mismatches)}\"}}";
'@
    Assert-ProbeTrue -Name 'カードが並ぶ' -Condition ([int]$cards.total -ge 1) -Detail "(実測: $($cards.total)枚 / タグあり $($cards.tagged)枚 / 流れる $($cards.scrolling)枚)" | Out-Null
    Assert-ProbeValue -Name 'カードのタグがマスターデータの並び順・表示名で入る枚数' -Actual ([double]$cards.matched) -Expected ([double]$cards.total) | Out-Null
    if ($cards.mismatches) { Write-Host "    不一致: $($cards.mismatches)" }
    Assert-ProbeValue -Name 'タグ名がキーのまま（TagText 未読込）の数' -Actual ([double]$cards.keyNames) -Expected 0 | Out-Null

    # --- 4. 収まらないタグは時間で流れ、枠の外は透明になる ---
    # どのカードが引かれても確かめられるよう、先頭のカードに長いタグを直接流し込む
    Invoke-UnityCode -Snippet @'
using System.Linq;
using UnityEngine;
using App.Battle.Views;

var board = UnityEngine.Object.FindObjectOfType<UpgradeCardBoardView>();
var card = UnityEngine.Object.FindObjectsOfType<UpgradeCardView>().OrderBy(c => c.Index).First();
board.SetTags(card.Index, new[] { "フォーカスコンフリクト", "アキンボコンフリクト", "クリティカル" });
return "set";
'@ | Out-Null

    $tickerSnippet = @'
using System.Linq;
using UnityEngine;
using App.Battle.Views;

var card = UnityEngine.Object.FindObjectsOfType<UpgradeCardView>().OrderBy(c => c.Index).First();
var text = card.transform.Find("TagText").GetComponent<TMPro.TextMeshPro>();
var info = text.textInfo;
var rect = text.rectTransform.rect;
var first = info.characterInfo[0];
var firstX = info.meshInfo[first.materialReferenceIndex].vertices[first.vertexIndex].x;

var outsideVisible = 0; var insideOpaque = 0;
for (var i = 0; i < info.characterCount; i++)
{
    var c = info.characterInfo[i];
    if (!c.isVisible) continue;
    var verts = info.meshInfo[c.materialReferenceIndex].vertices;
    var alpha = info.meshInfo[c.materialReferenceIndex].colors32[c.vertexIndex].a;
    var center = (verts[c.vertexIndex].x + verts[c.vertexIndex + 2].x) * 0.5f;
    if ((center < rect.xMin || center > rect.xMax) && alpha > 0) outsideVisible++;
    if (center > rect.xMin + 1f && center < rect.xMax - 1f && alpha == 255) insideOpaque++;
}

return $"{{\"firstX\":{firstX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"outsideVisible\":{outsideVisible},\"insideOpaque\":{insideOpaque},\"count\":{info.characterCount}}}";
'@
    $before = Invoke-UnityJson -Snippet $tickerSnippet
    Start-Sleep -Milliseconds 2500
    $after = Invoke-UnityJson -Snippet $tickerSnippet

    Assert-ProbeTrue -Name '長いタグは2周ぶん並べて流す' -Condition ([int]$before.count -gt 30) -Detail "(文字数: $($before.count))" | Out-Null
    Assert-ProbeTrue -Name '時間で左へ流れる' -Condition ([double]$after.firstX -lt [double]$before.firstX - 1) `
        -Detail "(先頭文字のx: $($before.firstX) → $($after.firstX))" | Out-Null
    Assert-ProbeValue -Name '表示枠の外で見えている文字の数' -Actual ([double]$after.outsideVisible) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '表示枠の内側の文字は不透明' -Condition ([int]$after.insideOpaque -ge 3) -Detail "(不透明な文字: $($after.insideOpaque))" | Out-Null

    # 見た目の確認用に Game ビューを撮っておく
    $shot = Invoke-Uloop -Command 'screenshot' -Params @{ 'window-name' = 'Game' }
    Write-Host "スクリーンショット: $($shot | ConvertTo-Json -Compress -Depth 4)"
}
