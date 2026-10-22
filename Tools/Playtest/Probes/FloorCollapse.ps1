#
# ステージ遷移用の床崩壊（FloorCollapseUseCase / FloorCollapseDataStore / FloorCollapseView）を検証するプローブ。
#
# 1. ウェーブクリア（制限時間の経過）で崩壊が始まり、目印の付いた床が隠れてプレート床が出る
#    - プレートの枚数は 分割密度（PlateSize）と広さ（FieldSize）どおり
#    - 当たり判定（Stage/Plane のコライダー）には触れない
# 2. 進行度がシェーダへ渡り、Duration 秒で完了して OnCompleted が流れる（完了後も床は崩れたまま）
# 3. 次のウェーブが始まると床が元に戻り、プレート床が消える
# 4. もう一度再生でき、同じ分割ならメッシュを作り直さない
#

function ProbePrepare {
    # 開いているシーンを切り替えず、Boot で初期化してから Battle を再生する（PlaytestCommon.ps1）
    Request-BootToBattle
}

function ProbeCleanup {
    Clear-BootToBattle
}

$Global:ProbeFloorCollapsePrelude = @'
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Common.Data;
using App.Battle.Interface.DataStore;
using App.Battle.Views.FloorCollapse;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var collapse = scope.Container.Resolve<IFloorCollapseDataStore>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var config = scope.Container.Resolve<FloorCollapseConfig>();
var view = UnityEngine.Object.FindObjectOfType<FloorCollapseView>();
var plates = view.transform.Find("Plates").GetComponent<MeshRenderer>();
var target = UnityEngine.Object.FindObjectOfType<FloorCollapseTargetView>().TargetRenderer;
var collider = GameObject.Find("Stage/Plane").GetComponent<Collider>();
var block = new MaterialPropertyBlock();
plates.GetPropertyBlock(block);
var ci = System.Globalization.CultureInfo.InvariantCulture;

'@

function Get-FloorCollapseState {
    return Invoke-UnityJson -Snippet ($Global:ProbeFloorCollapsePrelude + @'
var s = config.Settings;
var perSide = Mathf.CeilToInt(s.FieldSize / s.PlateSize);
return "{"
    + "\"phase\":\"" + collapse.Phase.CurrentValue + "\""
    + ",\"progress\":" + collapse.Progress.CurrentValue.ToString(ci)
    + ",\"shaderProgress\":" + block.GetFloat("_CollapseProgress").ToString(ci)
    + ",\"platesVisible\":" + (plates.enabled ? "true" : "false")
    + ",\"targetVisible\":" + (target.enabled ? "true" : "false")
    + ",\"plateCount\":" + view.PlateCount
    + ",\"expectedPlates\":" + (perSide * perSide)
    + ",\"meshId\":" + plates.GetComponent<MeshFilter>().sharedMesh.GetInstanceID()
    + ",\"colliderEnabled\":" + (collider.enabled ? "true" : "false")
    + ",\"colliderY\":" + collider.transform.position.y.ToString(ci)
    + ",\"duration\":" + s.Duration.ToString(ci)
    + ",\"wave\":" + wave.CurrentWave.CurrentValue
    + ",\"pause\":" + (wave.IsWavePause.Value ? "true" : "false")
    + "}";
'@)
}

# ウェーブの制限時間を経過させてウェーブクリアにする（WaveTimeOnly プローブと同じ方法）
function Invoke-WaveClear {
    Invoke-UnityCode -Snippet ($Global:ProbeFloorCollapsePrelude + @'
var waveConfig = scope.Container.Resolve<WaveConfig>();
wave.AddElapsedTime(waveConfig.WaveDurationSeconds);
return "ok";
'@) | Out-Null
}

# 次のウェーブを始める（ショップの「次のウェーブへ」と同じくポーズを解く）
function Invoke-NextWave {
    Invoke-UnityCode -Snippet ($Global:ProbeFloorCollapsePrelude + @'
wave.SetWavePause(false);
return "ok";
'@) | Out-Null
}

function ProbeRun {
    $before = Get-FloorCollapseState
    Assert-ProbeTrue -Name '開始前は床がそのまま（プレート床は出ていない）' `
        -Condition ($before.phase -eq 'Idle' -and $before.targetVisible -and -not $before.platesVisible) `
        -Detail "(phase=$($before.phase) target=$($before.targetVisible) plates=$($before.platesVisible))" | Out-Null

    # --- 1. ウェーブクリアで崩壊が始まる ---
    Invoke-WaveClear
    Start-Sleep -Milliseconds 300
    $started = Get-FloorCollapseState
    Assert-ProbeTrue -Name 'ウェーブクリアで崩壊が始まる' -Condition ($started.phase -eq 'Collapsing' -and $started.pause) `
        -Detail "(phase=$($started.phase) wave=$($started.wave) pause=$($started.pause))" | Out-Null
    Assert-ProbeTrue -Name '目印の床が隠れ、プレート床が出る' -Condition ((-not $started.targetVisible) -and $started.platesVisible) | Out-Null
    Assert-ProbeValue -Name 'プレートの枚数が分割密度と広さどおり' -Actual $started.plateCount -Expected $started.expectedPlates | Out-Null
    Assert-ProbeTrue -Name '当たり判定のコライダーには触れない' -Condition ($started.colliderEnabled -and $started.colliderY -eq $before.colliderY) | Out-Null

    # --- 2. 進行度がシェーダへ渡り、Duration 秒で完了する ---
    Start-Sleep -Seconds 1
    $mid = Get-FloorCollapseState
    Assert-ProbeTrue -Name '崩壊中は進行度が進む' -Condition ($mid.phase -eq 'Collapsing' -and $mid.progress -gt $started.progress -and $mid.progress -lt 1) `
        -Detail "(progress=$($mid.progress))" | Out-Null
    Assert-ProbeValue -Name 'シェーダへ渡した進行度が DataStore と一致' -Actual $mid.shaderProgress -Expected $mid.progress | Out-Null

    Start-Sleep -Seconds ([double]$mid.duration)
    $done = Get-FloorCollapseState
    Assert-ProbeTrue -Name 'Duration 秒で完了する' -Condition ($done.phase -eq 'Completed') -Detail "(phase=$($done.phase))" | Out-Null
    Assert-ProbeValue -Name '完了時の進行度は1' -Actual $done.shaderProgress -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '完了後も床は崩れたまま（次の演出へ渡す）' -Condition ($done.platesVisible -and -not $done.targetVisible) | Out-Null

    # --- 3. 次のウェーブで床が戻る ---
    Invoke-NextWave
    Start-Sleep -Milliseconds 300
    $restored = Get-FloorCollapseState
    Assert-ProbeTrue -Name '次のウェーブで床が元に戻る' `
        -Condition ($restored.phase -eq 'Idle' -and $restored.targetVisible -and -not $restored.platesVisible) `
        -Detail "(phase=$($restored.phase) target=$($restored.targetVisible) plates=$($restored.platesVisible))" | Out-Null

    # --- 4. もう一度再生でき、メッシュは作り直さない ---
    Invoke-WaveClear
    Start-Sleep -Milliseconds 300
    $again = Get-FloorCollapseState
    Assert-ProbeTrue -Name '2回目のウェーブクリアでも崩壊が始まる' -Condition ($again.phase -eq 'Collapsing' -and $again.platesVisible -and -not $again.targetVisible) | Out-Null
    Assert-ProbeValue -Name '同じ分割ならメッシュを作り直さない' -Actual $again.meshId -Expected $started.meshId | Out-Null
    Assert-ProbeTrue -Name '2回目は進行度0付近から始まる' -Condition ($again.progress -lt 0.2) -Detail "(progress=$($again.progress))" | Out-Null

    Invoke-NextWave
    Start-Sleep -Milliseconds 300
    $end = Get-FloorCollapseState
    Assert-ProbeTrue -Name '途中でウェーブが始まっても床が戻る' -Condition ($end.phase -eq 'Idle' -and $end.targetVisible -and -not $end.platesVisible) | Out-Null
}
