#
# ショップの通貨制（ShopUseCase / ShopView）を数値で検証するプローブ。
#
# 所持ポイントによる購入可否・購入時のポイント消費・1ショップ内での複数購入を、
# 実際にボタンを押して所持ポイントと取得済みアップグレード数の実測で確認する。
#
# 候補のコストは固定値を仮定せず、ボタンのラベル（"…\n<コスト> P"）から読む。
# デバッグ用の開始アップグレードが載っているとLv2以上（＝コストが異なる）の候補が並ぶため。
#

function ProbeRun {
    # --- 1. ポイント0ではどの候補も買えない ---
    $noPoint = Invoke-UnityJson -Snippet @'
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var waveDataStore = scope.Container.Resolve<IWaveManagerDataStore>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var shopView = scope.Container.Resolve<IShopView>() as MonoBehaviour;
if (shopView == null) throw new System.Exception("IShopViewを解決できません");

// ウェーブ突破と同じ手順でショップを開く
waveDataStore.SetWavePause(true);
waveDataStore.AdvanceWave();

var candidateCount = 0;
var interactableCount = 0;
foreach (var button in shopView.GetComponentsInChildren<Button>(true))
{
    if (!button.name.StartsWith("UpgradeButton") || !button.gameObject.activeInHierarchy) continue;
    candidateCount++;
    if (button.interactable) interactableCount++;
}

return $"{{\"point\":{pointDataStore.CurrentPoint.CurrentValue},\"candidateCount\":{candidateCount},\"interactableCount\":{interactableCount}}}";
'@

    Assert-ProbeValue -Name 'ショップを開いた時点の所持ポイント' -Actual ([double]$noPoint.point) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '候補が1件以上並ぶ' -Condition ([int]$noPoint.candidateCount -ge 1) `
        -Detail "(実測: $($noPoint.candidateCount)件)" | Out-Null
    Assert-ProbeValue -Name 'ポイント0のとき押せる候補の数' -Actual ([double]$noPoint.interactableCount) -Expected 0 | Out-Null

    # --- 2. ショップ表示中にポイントが入ると購入可否がその場で更新される ---
    #     （粒子はショップ表示中も吸い寄せられて回収されるため、開き直さずに反映される必要がある）
    $funded = Invoke-UnityJson -Snippet @'
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var shopView = scope.Container.Resolve<IShopView>() as MonoBehaviour;

// ショップは開いたまま、粒子を回収したのと同じようにポイントを足す
pointDataStore.Add(500);

var interactableCount = 0;
foreach (var button in shopView.GetComponentsInChildren<Button>(true))
{
    if (!button.name.StartsWith("UpgradeButton") || !button.gameObject.activeInHierarchy) continue;
    if (button.interactable) interactableCount++;
}

return $"{{\"point\":{pointDataStore.CurrentPoint.CurrentValue},\"interactableCount\":{interactableCount}}}";
'@

    Assert-ProbeValue -Name '付与後の所持ポイント' -Actual ([double]$funded.point) -Expected 500 | Out-Null
    Assert-ProbeTrue -Name '表示中にポイントが入ると候補を押せるようになる' `
        -Condition ([int]$funded.interactableCount -ge 2) -Detail "(押せる候補: $($funded.interactableCount)件)" | Out-Null

    # --- 3. 購入でコストぶん減り、同じショップで続けて買える ---
    $purchased = Invoke-UnityJson -Snippet @'
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var sessionDataStore = scope.Container.Resolve<IUpgradeSessionDataStore>();
var shopView = scope.Container.Resolve<IShopView>() as MonoBehaviour;

Button Find(string name)
{
    return shopView.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
}

// ラベル末尾の "<コスト> P" を読む（Viewが表示しているコストそのものを検証に使う）
int ReadCost(Button button)
{
    var lines = button.GetComponentInChildren<Text>().text.Split('\n');
    return int.Parse(lines[lines.Length - 1].Replace("P", "").Trim());
}

var ownedBefore = sessionDataStore.AppliedUpgrades.Count;
var pointBefore = pointDataStore.CurrentPoint.CurrentValue;

var firstCost = ReadCost(Find("UpgradeButton0"));
Find("UpgradeButton0").onClick.Invoke();
var pointAfterFirst = pointDataStore.CurrentPoint.CurrentValue;
var ownedAfterFirst = sessionDataStore.AppliedUpgrades.Count;
var firstButtonActive = Find("UpgradeButton0").gameObject.activeInHierarchy;

// 2件目の購入（1ウェーブ1回の制限を外したことの確認）
var secondCost = ReadCost(Find("UpgradeButton1"));
Find("UpgradeButton1").onClick.Invoke();
var pointAfterSecond = pointDataStore.CurrentPoint.CurrentValue;
var ownedAfterSecond = sessionDataStore.AppliedUpgrades.Count;

// 購入済みのボタンを押し直しても二重取得・二重支払いにならないこと
Find("UpgradeButton0").onClick.Invoke();
var ownedAfterRepress = sessionDataStore.AppliedUpgrades.Count;
var pointAfterRepress = pointDataStore.CurrentPoint.CurrentValue;

return $"{{\"ownedBefore\":{ownedBefore},\"pointBefore\":{pointBefore},\"firstCost\":{firstCost},\"pointAfterFirst\":{pointAfterFirst},\"ownedAfterFirst\":{ownedAfterFirst},\"firstButtonActive\":{firstButtonActive.ToString().ToLower()},\"secondCost\":{secondCost},\"pointAfterSecond\":{pointAfterSecond},\"ownedAfterSecond\":{ownedAfterSecond},\"ownedAfterRepress\":{ownedAfterRepress},\"pointAfterRepress\":{pointAfterRepress}}}";
'@

    Assert-ProbeTrue -Name '候補のコストが1以上表示される' -Condition ([int]$purchased.firstCost -ge 1) `
        -Detail "(1件目: $($purchased.firstCost) P / 2件目: $($purchased.secondCost) P)" | Out-Null
    Assert-ProbeValue -Name '1件目の購入で減ったポイント' `
        -Actual ([double]$purchased.pointBefore - [double]$purchased.pointAfterFirst) `
        -Expected ([double]$purchased.firstCost) | Out-Null
    Assert-ProbeValue -Name '1件目の購入で増えた取得数' `
        -Actual ([double]$purchased.ownedAfterFirst - [double]$purchased.ownedBefore) -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '購入した候補のボタンが消える' -Condition (-not [bool]$purchased.firstButtonActive) | Out-Null
    Assert-ProbeValue -Name '2件目の購入で減ったポイント' `
        -Actual ([double]$purchased.pointAfterFirst - [double]$purchased.pointAfterSecond) `
        -Expected ([double]$purchased.secondCost) | Out-Null
    Assert-ProbeValue -Name '同じショップで2件購入できる' `
        -Actual ([double]$purchased.ownedAfterSecond - [double]$purchased.ownedBefore) -Expected 2 | Out-Null
    Assert-ProbeValue -Name '購入済みボタンの再押下で取得数が増えない' `
        -Actual ([double]$purchased.ownedAfterRepress) -Expected ([double]$purchased.ownedAfterSecond) | Out-Null
    Assert-ProbeValue -Name '購入済みボタンの再押下でポイントが減らない' `
        -Actual ([double]$purchased.pointAfterRepress) -Expected ([double]$purchased.pointAfterSecond) | Out-Null

    # --- 4. 買えるだけ買うと、残りの候補は押せなくなる ---
    $spentOut = Invoke-UnityJson -Snippet @'
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var pointDataStore = scope.Container.Resolve<IPointDataStore>();
var sessionDataStore = scope.Container.Resolve<IUpgradeSessionDataStore>();
var shopView = scope.Container.Resolve<IShopView>() as MonoBehaviour;

int ReadCost(Button button)
{
    var lines = button.GetComponentInChildren<Text>().text.Split('\n');
    return int.Parse(lines[lines.Length - 1].Replace("P", "").Trim());
}

var ownedBefore = sessionDataStore.AppliedUpgrades.Count;
var pointBefore = pointDataStore.CurrentPoint.CurrentValue;
var spent = 0;

// 買えるだけ買う（押すたびに購入可否が更新される）
foreach (var button in shopView.GetComponentsInChildren<Button>(true)
             .Where(b => b.name.StartsWith("UpgradeButton") && b.gameObject.activeInHierarchy)
             .ToList())
{
    if (!button.interactable) continue;
    spent += ReadCost(button);
    button.onClick.Invoke();
}

var remaining = shopView.GetComponentsInChildren<Button>(true)
    .Where(b => b.name.StartsWith("UpgradeButton") && b.gameObject.activeInHierarchy)
    .ToList();

var currentPoint = pointDataStore.CurrentPoint.CurrentValue;
// 残った候補が「買えないから残っている」ことを確かめる
var affordableRemain = remaining.Count(b => ReadCost(b) <= currentPoint);
var interactableRemain = remaining.Count(b => b.interactable);
var ownedDelta = sessionDataStore.AppliedUpgrades.Count - ownedBefore;

return $"{{\"point\":{currentPoint},\"pointBefore\":{pointBefore},\"spent\":{spent},\"remainCount\":{remaining.Count},\"interactableCount\":{interactableRemain},\"affordableRemain\":{affordableRemain},\"ownedDelta\":{ownedDelta}}}";
'@

    Assert-ProbeValue -Name '購入した合計コストぶんポイントが減る' `
        -Actual ([double]$spentOut.pointBefore - [double]$spentOut.point) -Expected ([double]$spentOut.spent) | Out-Null
    Assert-ProbeValue -Name '残った候補に買えるものは無い' -Actual ([double]$spentOut.affordableRemain) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '買えなくなった候補は押せない' -Actual ([double]$spentOut.interactableCount) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name 'まとめ買いで複数取得できている' -Condition ([int]$spentOut.ownedDelta -ge 1) `
        -Detail "(このステップの取得数: $($spentOut.ownedDelta)件)" | Out-Null
}
