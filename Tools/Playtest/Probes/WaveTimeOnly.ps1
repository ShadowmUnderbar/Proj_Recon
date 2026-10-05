#
# 通常ウェーブが制限時間の経過だけで進むこと（WaveManagerUseCase / WaveConfig）を検証するプローブ。
#
# 以前は撃破数（WaveEnemyKillCount）でも進んでいたため、撃破数を大きく積んでもウェーブが進まないこと、
# 経過時間が WaveDurationSeconds に達したら進むことを実測値で確認する。
#

function ProbePrepare {
    # 開いているシーンを切り替えず、再生開始時だけ Battle シーンを使う（TutorialWave プローブと同じ）
    $Global:ProbeSavedPlayModeStartScene = Invoke-UnityCode -Snippet @'
using UnityEditor;
using UnityEditor.SceneManagement;

var saved = EditorSceneManager.playModeStartScene != null
    ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
    : string.Empty;
EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Battle.unity");

return saved;
'@
    Write-Host "再生開始シーンを Battle に設定しました（退避: [$Global:ProbeSavedPlayModeStartScene]）"
}

function ProbeCleanup {
    $saved = $Global:ProbeSavedPlayModeStartScene
    $escaped = $saved -replace '\\', '\\' -replace '"', '\"'

    Invoke-UnityCode -Snippet @"
using UnityEditor;
using UnityEditor.SceneManagement;

var path = "$escaped";
EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path)
    ? null
    : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

return "restored";
"@ | Out-Null

    Write-Host "再生開始シーンを戻しました: [$saved]"
}

# 旧仕様の目標撃破数（20）を十分に超える数
$Global:ProbeWaveTimeOnlyKillCount = 100

function ProbeRun {
    $before = Get-WaveState
    Assert-ProbeTrue -Name 'ウェーブ1が進行中' -Condition ($before.currentWave -eq 1 -and -not $before.isWavePause) `
        -Detail "(wave=$($before.currentWave) pause=$($before.isWavePause))" | Out-Null

    # --- 1. 撃破数を大きく積んでもウェーブは進まない ---
    # 進行判定は毎フレームの Tick で行うため、積んだあと数フレーム待ってから見る
    Invoke-UnityCode -Snippet @"
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
for (var i = 0; i < $Global:ProbeWaveTimeOnlyKillCount; i++)
{
    wave.IncrementKillCount();
}
return "ok";
"@ | Out-Null
    Start-Sleep -Seconds 1

    $afterKill = Get-WaveState
    Assert-ProbeTrue -Name '撃破数を積んでもウェーブ1のまま' `
        -Condition ($afterKill.currentWave -eq 1 -and -not $afterKill.isWavePause -and $afterKill.kill -ge $Global:ProbeWaveTimeOnlyKillCount) `
        -Detail "(wave=$($afterKill.currentWave) pause=$($afterKill.isWavePause) kill=$($afterKill.kill))" | Out-Null

    # --- 2. 経過時間が制限時間に達するとウェーブが進む ---
    $duration = Invoke-UnityCode -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;
using App.Common.Data;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var config = scope.Container.Resolve<WaveConfig>();
wave.AddElapsedTime(config.WaveDurationSeconds);
return config.WaveDurationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
'@
    Start-Sleep -Seconds 1

    $afterTime = Get-WaveState
    Assert-ProbeTrue -Name "経過時間が制限時間（${duration}秒）に達したらウェーブ2へ進む" `
        -Condition ($afterTime.currentWave -eq 2 -and $afterTime.isWavePause) `
        -Detail "(wave=$($afterTime.currentWave) pause=$($afterTime.isWavePause))" | Out-Null
}
