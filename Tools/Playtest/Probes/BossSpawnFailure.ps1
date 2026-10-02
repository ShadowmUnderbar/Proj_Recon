#
# ボスのプレハブの読み込みに失敗したとき、ボスウェーブから抜けられなくならないことを確かめるプローブ。
#
# BossGroup_TickTock のメンバーのマスターデータ（B-002）の PrefabPath を、実行中だけ存在しないパスへ差し替える。
#   - 読み込みに失敗した敵は、敵データごと取り除かれる（撃破扱いにはしない＝撃破の通知は来ない）
#   - ボスグループの台本は取り除かれた個体を対象から外し、全員いなくなるのでボスウェーブが進む
#   - 体力ゲージなどボス用の表示も残らない
# 読み込み失敗のエラーログ（EnemyStoreView の「敵プレハブの読み込みに失敗しました」と Addressables の例外）は想定どおり。
# 最後に自分でエラーログを読み、想定外のエラーが無いことを確かめてからコンソールを消す（ランナーのエラー判定に数えさせない）。
#

. (Join-Path $PSScriptRoot 'Common/BossProbeCommon.ps1')

$Global:SpawnFailureGroupPath = 'Assets/App/MasterData/Boss/BossGroup_TickTock.asset'
$Global:SpawnFailureMasterPath = 'Assets/App/MasterData/Enemy/BossTickTock.asset'
$Global:SpawnFailureBrokenPrefabPath = 'Assets/App/Prefub/Enemy/__MissingBossForProbe__.prefab'

# 想定どおりに出るエラーログ（読み込み失敗）と見なす文字列
$Global:SpawnFailureExpectedErrorPatterns = @(
    '敵プレハブの読み込みに失敗しました',
    'InvalidKeyException',
    '__MissingBossForProbe__'
)

function ProbePrepare {
    Enter-BossProbeScene
    try {
        $Global:SpawnFailureSavedGroup = Set-BossWaveGroup -GroupPath $Global:SpawnFailureGroupPath
        $Global:SpawnFailureSavedPrefab = Invoke-UnityCode -Snippet @"
using UnityEditor;

var md = AssetDatabase.LoadAssetAtPath<App.Common.Data.MasterData.EnemyMasterData>("$($Global:SpawnFailureMasterPath)");
var so = new SerializedObject(md);
var prop = so.FindProperty("_prefabPath");
var saved = prop.stringValue;
prop.stringValue = "$($Global:SpawnFailureBrokenPrefabPath)";
so.ApplyModifiedPropertiesWithoutUndo();
return saved;
"@
        Write-Host "ボスのプレハブのパスを壊しました（退避: [$Global:SpawnFailureSavedPrefab]）"
    }
    catch {
        Exit-BossProbeScene
        throw
    }
}

function ProbeCleanup {
    Invoke-UnityCode -Snippet @"
using UnityEditor;

var md = AssetDatabase.LoadAssetAtPath<App.Common.Data.MasterData.EnemyMasterData>("$($Global:SpawnFailureMasterPath)");
var so = new SerializedObject(md);
so.FindProperty("_prefabPath").stringValue = "$($Global:SpawnFailureSavedPrefab)";
so.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssets();
return "restored";
"@ | Out-Null
    Write-Host "ボスのプレハブのパスを戻しました: [$Global:SpawnFailureSavedPrefab]"
    Restore-BossWaveGroup -GroupPath $Global:SpawnFailureSavedGroup
    Exit-BossProbeScene
}

function ProbeRun {
    try {
        Invoke-SpawnFailureBody
    }
    finally {
        Stop-BossProbeRecorder
    }
}

function Invoke-SpawnFailureBody {
    $bossWaveNumber = Move-ToBossWaveShop

    # 撃破・消去の通知を記録してからボスウェーブを始める
    Invoke-BossSnippet -Body @'
var dead = new List<int>();
var removed = new List<int>();
var subs = new CompositeDisposable();
enemies.OnEnemyDead.Subscribe(id => dead.Add(id)).AddTo(subs);
enemies.OnEnemyRemoved.Subscribe(id => removed.Add(id)).AddTo(subs);
Observable.EveryUpdate().Subscribe(_ => player.Health.Value = player.MaxHealth.Value).AddTo(subs);
AppDomain.CurrentDomain.SetData("bossProbe.sub", subs);
AppDomain.CurrentDomain.SetData("bossProbe.dead", dead);
AppDomain.CurrentDomain.SetData("bossProbe.removed", removed);
return "{}";
'@ | Out-Null

    Skip-BossProbeShop
    Start-Sleep -Seconds 3

    $result = Invoke-BossSnippet -Body @'
var dead = (List<int>)AppDomain.CurrentDomain.GetData("bossProbe.dead");
var removed = (List<int>)AppDomain.CurrentDomain.GetData("bossProbe.removed");
var bossLeft = enemies.Enemies.Count(IsBossEnemy);
var store = UnityEngine.Object.FindObjectOfType<App.Battle.Views.BossLifeGaugeStoreView>();
var gauges = store == null ? -1 : store.transform.childCount;
var bossViews = UnityEngine.Object.FindObjectsOfType<BossAIBase>().Length;
return $"{{\"dead\":{dead.Count},\"removed\":{removed.Count},\"bossLeft\":{bossLeft},\"gauges\":{gauges},\"bossViews\":{bossViews},\"hasAliveGroup\":{bossGroups.HasAliveGroup.ToString().ToLower()}}}";
'@
    Assert-ProbeValue -Name '読み込みに失敗したボスの敵データが残らない' -Actual $result.bossLeft -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '2体とも取り除かれる（消去の通知）' -Condition ($result.removed -ge 2) -Detail "消去 $($result.removed) 件" | Out-Null
    Assert-ProbeValue -Name '撃破扱いにはしない（撃破の通知）' -Actual $result.dead -Expected 0 | Out-Null
    Assert-ProbeTrue -Name 'ボスグループが残らない' -Condition (-not $result.hasAliveGroup) | Out-Null
    Assert-ProbeValue -Name 'ボスのビューは作られない' -Actual $result.bossViews -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'ボスの体力ゲージが残らない' -Actual $result.gauges -Expected 0 | Out-Null

    $state = Get-WaveState
    # ボスウェーブは開始と同時に終わってショップが開く。Skip-BossProbeShop は押した直後にポーズへ戻ったのを見て押し直すため、
    # 次のウェーブがすでに始まっていることがある（ポーズの状態は問わない）
    Assert-ProbeTrue -Name 'ボスウェーブで止まらず次のウェーブへ進む' -Condition ($state.currentWave -eq ($bossWaveNumber + 1)) -Detail "wave=$($state.currentWave) pause=$($state.isWavePause)" | Out-Null

    # エラーログ: 読み込み失敗のぶんだけ出ていること（想定外のエラーが無いこと）を自分で確かめ、ランナーの判定に数えさせないよう消す
    $errors = @((Invoke-Uloop -Command 'get-logs' -Params @{ 'log-type' = 'Error'; 'max-count' = '50' }).Logs | Where-Object { $null -ne $_ })
    $expected = @($errors | Where-Object { $msg = "$($_.Message)"; @($Global:SpawnFailureExpectedErrorPatterns | Where-Object { $msg.Contains($_) }).Count -gt 0 })
    $unexpected = @($errors | Where-Object { $msg = "$($_.Message)"; @($Global:SpawnFailureExpectedErrorPatterns | Where-Object { $msg.Contains($_) }).Count -eq 0 })
    Assert-ProbeTrue -Name '読み込み失敗のエラーログが出ている（気付ける）' -Condition ($expected.Count -ge 1) -Detail "$($expected.Count) 件" | Out-Null
    Assert-ProbeValue -Name '想定外のエラーログが無い' -Actual $unexpected.Count -Expected 0 | Out-Null
    $unexpected | ForEach-Object { Write-Host "    想定外: $($_.Message)" }
    Invoke-Uloop -Command 'clear-console' | Out-Null
}
