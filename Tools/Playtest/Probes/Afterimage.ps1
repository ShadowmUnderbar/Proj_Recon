#
# プレイヤーの残像（AfterimageUseCase / AfterimageStoreView / PlayerAfterimageSourceView）を検証するプローブ。
#
# 時間を止めた状態（Time.timeScale = 0）で残像を残し、寿命を進めずに中身を読む。
# 1. 体と頭（テスト用に頭の取り付け位置へ足したキューブ）が1体の残像として写し取られる
#    - 頭は MeshRenderer の共有メッシュと、非一様スケール・回転を含む親階層の localToWorldMatrix をそのまま持つ
#    - 体は焼いたメッシュをワールドへ戻すと、頭のボーンと足元の位置に合う
# 2. 頭を差し替えても、先に残した残像の頭は変わらない（新しい残像だけが新しい頭になる）
# 3. 上限数を超えて残しても、残像と焼き込み用メッシュの数は上限で止まる（使い回される）
# 4. 時間を戻すと寿命で全部消え、形状を手放す
# 5. 一時強化なしで動いても残らない／レイジ発動中は動くと残り、立ち止まっていても時間で残る／レイジが切れると残らない
# 6. 刻み（AfterimageSpawnScheduler 単体）: 距離・時間のどちらか先で残す／ワープでは残さず数え直す／経過時間 0 なら時間では残さない
#

function ProbePrepare {
    # 開いているシーンを切り替えず、Boot で初期化してから Battle を再生する（PlaytestCommon.ps1）
    Request-BootToBattle
}

function ProbeCleanup {
    Clear-BootToBattle
}

# 各スニペット共通の取り出し（残像の一覧はストアの非公開フィールドから読む）
$Global:ProbeAfterimagePrelude = @'
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using App.Battle.Data;
using App.Battle.Views.Afterimage;

var store = UnityEngine.Object.FindObjectOfType<AfterimageStoreView>();
var source = UnityEngine.Object.FindObjectOfType<PlayerAfterimageSourceView>();
var instances = (List<AfterimageInstance>)typeof(AfterimageStoreView)
    .GetField("_instances", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(store);
var anchor = (Transform)typeof(PlayerAfterimageSourceView)
    .GetField("_headAnchor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(source);
var body = (Renderer)typeof(PlayerAfterimageSourceView)
    .GetField("_bodyRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(source);
var ci = System.Globalization.CultureInfo.InvariantCulture;

'@

# 状態を動かすスニペット共通の取り出し（プレイヤーの位置・バフ状態）
$Global:ProbeAfterimageScopePrelude = @'
using VContainer;
using VContainer.Unity;
using UnityEngine;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var state = scope.Container.Resolve<IPlayerStateDataStore>();
var buff = scope.Container.Resolve<IBuffStateDataStore>();

'@

# 頭の取り付け位置の下へ、非一様スケール・回転を持つ親ごとテスト用の頭を作る
function New-ProbeHeadSnippet([string]$name, [string]$primitive) {
    $snippet = @'
var root = new GameObject("__NAME__");
root.transform.SetParent(anchor, false);
root.transform.localPosition = new Vector3(0f, 0.15f, 0.02f);
root.transform.localRotation = Quaternion.Euler(20f, 35f, 0f);
root.transform.localScale = new Vector3(1f, 1.8f, 0.6f);
var head = GameObject.CreatePrimitive(PrimitiveType.__PRIMITIVE__);
UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
head.transform.SetParent(root.transform, false);
head.transform.localRotation = Quaternion.Euler(0f, 45f, 10f);
head.transform.localScale = new Vector3(0.25f, 0.2f, 0.3f);

'@
    return $Global:ProbeAfterimagePrelude + $snippet.Replace('__NAME__', $name).Replace('__PRIMITIVE__', $primitive)
}

function ProbeRun {
    # --- 1. 体と頭を1体の残像として写し取る ---
    $spawn1 = Invoke-UnityCode -Snippet ((New-ProbeHeadSnippet 'ProbeHeadCube' 'Cube') + @'
Time.timeScale = 0f;
store.Spawn(source, AfterimageStyle.Default);
return "ok";
'@)
    Assert-ProbeTrue -Name 'テスト用の頭（キューブ）を付けて残像を頼めた' -Condition ($spawn1 -eq 'ok') -Detail "($spawn1)" | Out-Null
    Start-Sleep -Milliseconds 500

    $first = Invoke-UnityJson -Snippet ($Global:ProbeAfterimagePrelude + @'
var alive = instances.Where(x => x.IsAlive).ToList();
var cube = GameObject.Find("ProbeHeadCube").transform.GetChild(0);
var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
var snapshot = alive[0].Snapshot;
var cubePart = snapshot.Parts.First(p => p.Mesh == cubeMesh);
var expected = cube.localToWorldMatrix;
var matrixDiff = 0f;
for (var i = 0; i < 16; i++) matrixDiff = Mathf.Max(matrixDiff, Mathf.Abs(cubePart.LocalToWorld[i] - expected[i]));

// 体の焼いたメッシュをワールドへ戻し、頭のボーンに一番近い頂点の距離と、一番低い頂点の高さを測る
var bodyPart = snapshot.Parts[0];
var vertices = bodyPart.Mesh.vertices;
var headDistance = float.MaxValue;
var minY = float.MaxValue;
foreach (var v in vertices)
{
    var w = bodyPart.LocalToWorld.MultiplyPoint3x4(v);
    headDistance = Mathf.Min(headDistance, Vector3.Distance(w, anchor.position));
    minY = Mathf.Min(minY, w.y);
}
var bodySubMeshes = ((SkinnedMeshRenderer)body).sharedMesh.subMeshCount;
var playerRoot = source.transform.position;
return "{"
    + "\"alive\":" + alive.Count
    + ",\"parts\":" + snapshot.Parts.Count
    + ",\"expectedParts\":" + (bodySubMeshes + cubeMesh.subMeshCount)
    + ",\"bodyBaked\":" + (bodyPart.Mesh != ((SkinnedMeshRenderer)body).sharedMesh ? "true" : "false")
    + ",\"matrixDiff\":" + matrixDiff.ToString(ci)
    + ",\"headDistance\":" + headDistance.ToString(ci)
    + ",\"footGap\":" + Mathf.Abs(minY - playerRoot.y).ToString(ci)
    + ",\"pivotDiff\":" + Vector3.Distance(snapshot.Pivot, body.transform.position).ToString(ci)
    + "}";
'@)
    Assert-ProbeValue -Name '生きている残像は1体' -Actual $first.alive -Expected 1 | Out-Null
    Assert-ProbeValue -Name '体のサブメッシュ数＋頭のサブメッシュ数のパーツを持つ' -Actual $first.parts -Expected $first.expectedParts | Out-Null
    Assert-ProbeTrue -Name '体は元の共有メッシュではなく焼いたメッシュ' -Condition $first.bodyBaked | Out-Null
    Assert-ProbeValue -Name '頭の行列が元の localToWorldMatrix と一致（非一様スケール・回転の親を含む）' -Actual $first.matrixDiff -Expected 0 -Tolerance 0.0001 | Out-Null
    Assert-ProbeTrue -Name '体の焼いたメッシュが頭のボーンの近く（12cm以内）まで届く' -Condition ($first.headDistance -lt 0.12) -Detail "($($first.headDistance)m)" | Out-Null
    Assert-ProbeTrue -Name '体の焼いたメッシュの足元がプレイヤーの足元に合う（5cm以内）' -Condition ($first.footGap -lt 0.05) -Detail "($($first.footGap)m)" | Out-Null
    Assert-ProbeValue -Name '基準点が体の位置' -Actual $first.pivotDiff -Expected 0 -Tolerance 0.0001 | Out-Null

    # --- 2. 頭を差し替えても、先に残した残像は変わらない ---
    $before = Invoke-UnityCode -Snippet ($Global:ProbeAfterimagePrelude + @'
var cubeRoot = GameObject.Find("ProbeHeadCube");
var m = cubeRoot.transform.GetChild(0).localToWorldMatrix;
var values = string.Join(",", Enumerable.Range(0, 16).Select(i => m[i].ToString("R", ci)));
// 遅延破棄だと同じフレームの写し取りに古い頭が残るため、即時に消してから付け替える
UnityEngine.Object.DestroyImmediate(cubeRoot);
return values;
'@)
    $swap = Invoke-UnityCode -Snippet ((New-ProbeHeadSnippet 'ProbeHeadSphere' 'Sphere') + @'
store.Spawn(source, AfterimageStyle.Default);
return "ok";
'@)
    Assert-ProbeTrue -Name '頭をスフィアへ差し替えて2体目を頼めた' -Condition ($swap -eq 'ok') -Detail "($swap)" | Out-Null
    Start-Sleep -Milliseconds 500

    $swappedSnippet = @'
var recorded = "__RECORDED__".Split(',').Select(s => float.Parse(s, ci)).ToArray();
var alive = instances.Where(x => x.IsAlive).OrderBy(x => x.Seed).ToList();
var cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
var sphereMesh = GameObject.Find("ProbeHeadSphere").transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
var oldSnapshot = alive[0].Snapshot;
var newSnapshot = alive[alive.Count - 1].Snapshot;
var oldCube = oldSnapshot.Parts.Where(p => p.Mesh == cubeMesh).ToList();
var diff = 0f;
if (oldCube.Count > 0) for (var i = 0; i < 16; i++) diff = Mathf.Max(diff, Mathf.Abs(oldCube[0].LocalToWorld[i] - recorded[i]));
return "{"
    + "\"alive\":" + alive.Count
    + ",\"oldHasCube\":" + (oldCube.Count > 0 ? "true" : "false")
    + ",\"oldHasSphere\":" + (oldSnapshot.Parts.Any(p => p.Mesh == sphereMesh) ? "true" : "false")
    + ",\"oldMatrixDiff\":" + diff.ToString(ci)
    + ",\"newHasCube\":" + (newSnapshot.Parts.Any(p => p.Mesh == cubeMesh) ? "true" : "false")
    + ",\"newHasSphere\":" + (newSnapshot.Parts.Any(p => p.Mesh == sphereMesh) ? "true" : "false")
    + "}";
'@
    $swapped = Invoke-UnityJson -Snippet ($Global:ProbeAfterimagePrelude + $swappedSnippet.Replace('__RECORDED__', $before))
    Assert-ProbeValue -Name '差し替え後も残像は2体' -Actual $swapped.alive -Expected 2 | Out-Null
    Assert-ProbeTrue -Name '先の残像の頭はキューブのまま' -Condition ($swapped.oldHasCube -and -not $swapped.oldHasSphere) | Out-Null
    Assert-ProbeValue -Name '先の残像の頭の行列は変わらない' -Actual $swapped.oldMatrixDiff -Expected 0 -Tolerance 0.0001 | Out-Null
    Assert-ProbeTrue -Name '新しい残像の頭はスフィア' -Condition ($swapped.newHasSphere -and -not $swapped.newHasCube) | Out-Null

    # --- 3. 上限を超えて残しても使い回される ---
    # 同じフレームに頼んだ分は1体にまとめるため、1回ずつフレームを空けて頼む
    for ($i = 0; $i -lt 8; $i++) {
        Invoke-UnityCode -Snippet ($Global:ProbeAfterimagePrelude + @'
store.Spawn(source, AfterimageStyle.Default);
return "ok";
'@) | Out-Null
        Start-Sleep -Milliseconds 100
    }
    $pooled = Invoke-UnityJson -Snippet ($Global:ProbeAfterimagePrelude + @'
var config = (AfterimageConfig)typeof(AfterimageStoreView)
    .GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(store);
var bakedMeshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "AfterimageBakedMesh");
return "{"
    + "\"max\":" + config.MaxCount
    + ",\"instances\":" + instances.Count
    + ",\"alive\":" + instances.Count(x => x.IsAlive)
    + ",\"bakedMeshes\":" + bakedMeshes
    + "}";
'@)
    Assert-ProbeValue -Name '残像の数は上限で止まる' -Actual $pooled.instances -Expected $pooled.max | Out-Null
    Assert-ProbeValue -Name '上限まで表示中' -Actual $pooled.alive -Expected $pooled.max | Out-Null
    Assert-ProbeValue -Name '焼き込み用メッシュも上限ぶん（体1枚×残像数）で増えない' -Actual $pooled.bakedMeshes -Expected $pooled.max | Out-Null

    # --- 4. 時間を戻すと寿命で消え、形状を手放す ---
    Invoke-UnityCode -Snippet ($Global:ProbeAfterimagePrelude + @'
UnityEngine.Object.DestroyImmediate(GameObject.Find("ProbeHeadSphere"));
Time.timeScale = 1f;
return "ok";
'@) | Out-Null
    Start-Sleep -Seconds 2
    $expired = Invoke-UnityJson -Snippet ($Global:ProbeAfterimagePrelude + @'
return "{"
    + "\"alive\":" + instances.Count(x => x.IsAlive)
    + ",\"parts\":" + instances.Sum(x => x.Snapshot.Parts.Count)
    + "}";
'@)
    Assert-ProbeValue -Name '寿命で全部消える' -Actual $expired.alive -Expected 0 | Out-Null
    Assert-ProbeValue -Name '消えた残像は形状を手放す' -Actual $expired.parts -Expected 0 | Out-Null

    # --- 5. 一時強化の発動中だけ残像が残る ---
    # 寿命が短いので、動かすのと数えるのを同じスニペットで行わず、直後の呼び出しで数える（寿命内に収まる）。
    # 発動の検知は次フレームの Tick なので、発動させる呼び出しと動かす呼び出しも分ける
    $moveSnippet = $Global:ProbeAfterimageScopePrelude + @'
state.Position.Value += Vector3.forward * 3f;
return "ok";
'@
    $countSnippet = $Global:ProbeAfterimagePrelude + @'
return instances.Count(x => x.IsAlive).ToString();
'@

    Invoke-UnityCode -Snippet $moveSnippet | Out-Null
    Start-Sleep -Seconds 1
    $unbuffed = Invoke-UnityCode -Snippet $countSnippet
    Assert-ProbeValue -Name '一時強化なしでは、動いても時間がたっても残らない' -Actual ([int]$unbuffed) -Expected 0 | Out-Null

    Invoke-UnityCode -Snippet ($Global:ProbeAfterimageScopePrelude + @'
var rage = UnityEditor.AssetDatabase.LoadAssetAtPath<App.Common.Data.MasterData.BuffMasterData>("Assets/App/MasterData/Buff/RageBuffL1.asset");
buff.GainRage(30f, rage);
return "ok";
'@) | Out-Null
    Invoke-UnityCode -Snippet $moveSnippet | Out-Null
    $buffed = Invoke-UnityCode -Snippet $countSnippet
    Assert-ProbeTrue -Name 'レイジ発動中に動くと残る' -Condition ([int]$buffed -ge 1) -Detail "(表示中 $buffed 体)" | Out-Null

    # 立ち止まったまま、寿命より長く待っても残像が絶えない（時間で残し続ける）
    Start-Sleep -Seconds 2
    $standingCount = [int](Invoke-UnityCode -Snippet $countSnippet)
    Assert-ProbeTrue -Name 'レイジ発動中は立ち止まっていても残る' -Condition ($standingCount -ge 1) -Detail "(表示中 $standingCount 体)" | Out-Null

    # レイジの残り時間を0にして切れさせ、寿命が過ぎてから動かす
    $rageEnded = Invoke-UnityCode -Snippet ($Global:ProbeAfterimageScopePrelude + @'
typeof(App.Battle.DataStore.BuffStateDataStore)
    .GetField("_rageRemainingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
    .SetValue(buff, 0f);
return buff.IsTimedBuffActive ? "active" : "ok";
'@)
    Assert-ProbeTrue -Name 'レイジを切ると一時強化なしに戻る' -Condition ($rageEnded -eq 'ok') -Detail "($rageEnded)" | Out-Null
    Start-Sleep -Seconds 2
    Invoke-UnityCode -Snippet $moveSnippet | Out-Null
    $ended = Invoke-UnityCode -Snippet $countSnippet
    Assert-ProbeValue -Name 'レイジが切れると残らない' -Actual ([int]$ended) -Expected 0 | Out-Null

    # --- 6. 刻みの判定（AfterimageSpawnScheduler 単体） ---
    # 実時間・実フレームに頼らず、距離と経過時間を与えて判定だけを見る
    $scheduler = Invoke-UnityJson -Snippet @'
using UnityEngine;
using App.Battle.Data;

const float distance = 1.2f;
const float interval = 0.3f;
const float warp = 5f;
var s = new AfterimageSpawnScheduler();
var inactive = s.Advance(Vector3.forward * 10f, 1f, distance, interval, warp);
s.Begin(Vector3.zero);
var short1 = s.Advance(Vector3.zero, 0.2f, distance, interval, warp);
var byTime = s.Advance(Vector3.zero, 0.2f, distance, interval, warp);
var afterTime = s.Advance(Vector3.zero, 0.1f, distance, interval, warp);
var byDistance = s.Advance(Vector3.forward * 1.3f, 0.01f, distance, interval, warp);
var warped = s.Advance(Vector3.forward * 20f, 0.25f, distance, interval, warp);
var afterWarp = s.Advance(Vector3.forward * 20f, 0.1f, distance, interval, warp);
var paused = s.Advance(Vector3.forward * 20f, 0f, distance, interval, warp);
var resumed = s.Advance(Vector3.forward * 20f, 0.25f, distance, interval, warp);
s.End();
var ended = s.Advance(Vector3.forward * 40f, 1f, distance, interval, warp);
string B(bool b) => b ? "true" : "false";
return "{\"inactive\":" + B(inactive) + ",\"short1\":" + B(short1) + ",\"byTime\":" + B(byTime)
    + ",\"afterTime\":" + B(afterTime) + ",\"byDistance\":" + B(byDistance) + ",\"warped\":" + B(warped)
    + ",\"afterWarp\":" + B(afterWarp) + ",\"paused\":" + B(paused) + ",\"resumed\":" + B(resumed)
    + ",\"ended\":" + B(ended) + "}";
'@
    Assert-ProbeTrue -Name '刻み: Begin 前は残さない' -Condition (-not $scheduler.inactive) | Out-Null
    Assert-ProbeTrue -Name '刻み: 立ち止まって間隔未満なら残さない' -Condition (-not $scheduler.short1) | Out-Null
    Assert-ProbeTrue -Name '刻み: 立ち止まっていても間隔がたてば残す' -Condition ($scheduler.byTime) | Out-Null
    Assert-ProbeTrue -Name '刻み: 残したら時間を数え直す' -Condition (-not $scheduler.afterTime) | Out-Null
    Assert-ProbeTrue -Name '刻み: 間隔前でも距離を進めば残す' -Condition ($scheduler.byDistance) | Out-Null
    Assert-ProbeTrue -Name '刻み: ワープでは間隔を過ぎていても残さない' -Condition (-not $scheduler.warped) | Out-Null
    Assert-ProbeTrue -Name '刻み: ワープ後は時間も数え直す' -Condition (-not $scheduler.afterWarp) | Out-Null
    Assert-ProbeTrue -Name '刻み: 経過時間 0（ショップ・フリーズ・時止め）では時間で残さない' -Condition (-not $scheduler.paused) | Out-Null
    Assert-ProbeTrue -Name '刻み: 経過時間が戻れば時間で残す' -Condition ($scheduler.resumed) | Out-Null
    Assert-ProbeTrue -Name '刻み: End 後は残さない' -Condition (-not $scheduler.ended) | Out-Null
}
