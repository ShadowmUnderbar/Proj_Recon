#
# ステージ遷移用の床崩壊（FloorCollapseUseCase / FloorCollapseDataStore / FloorCollapseView）を検証するプローブ。
#
# 1. ウェーブクリア（制限時間の経過）で崩壊が始まり、目印の付いた床が隠れてプレート床が出る
#    - プレートの枚数は 分割密度（PlateSize）と広さ（FieldSize）どおり
#    - 当たり判定（Stage/Plane のコライダー）には触れない
# 2. 進行度がシェーダへ渡り、Duration 秒で完了して OnCompleted が流れる（完了後も床は崩れたまま）
#    - 崩壊中はショップが開かず、フォーカス入力が止まる（移動・射撃・回避はウェーブ間ポーズで止まっている）
#    - 開始（OnStarted）と終了（OnCompleted）が1回ずつ流れ、終了と同時にショップが開く
# 3. 次のウェーブ（ショップの「次のウェーブへ」）で床が元に戻り、プレート床が消える
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
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
// ショップが開いているかは「次のウェーブへ」ボタンが出ているかで見る（PlaytestCommon.ps1 と同じパス）
var nextWaveButton = GameObject.Find("BattleLifetimeScope/ShopView(Clone)/ShopCanvas/Panel/NextWaveButton");
var isShopOpen = nextWaveButton != null && nextWaveButton.activeInHierarchy;
var input = scope.Container.Resolve<App.Common.Interface.IGameInputDataStore>();
var isFocusInputEnabled = (bool)typeof(App.Common.DataStore.GameInputDataStore).GetField("_isFocusInputEnabled", flags).GetValue(input);
var started = System.AppDomain.CurrentDomain.GetData("ProbeFloorCollapseStarted") as int? ?? 0;
var completed = System.AppDomain.CurrentDomain.GetData("ProbeFloorCollapseCompleted") as int? ?? 0;

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
    + ",\"shopOpen\":" + (isShopOpen ? "true" : "false")
    + ",\"focusInput\":" + (isFocusInputEnabled ? "true" : "false")
    + ",\"started\":" + started
    + ",\"completed\":" + completed
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

# 次のウェーブを始める。ショップが開いていれば「次のウェーブへ」ボタンを押し、開いていなければポーズだけ解く
function Invoke-NextWave {
    $state = Get-FloorCollapseState
    if ($state.shopOpen) {
        # 押してもポーズが解けなければ押し直す（Resolve-ShopIfOpen と同じ）
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
                action = 'Click'; 'target-path' = $Global:PlaytestShopNextWaveButtonPath; 'bypass-raycast' = 'true'
            } | Out-Null
            Start-Sleep -Milliseconds 500
            if (-not (Get-FloorCollapseState).pause) { return }
        }
        throw "ショップから次ウェーブへ遷移できませんでした（NextWaveButton押下後もポーズのまま）"
    }

    Invoke-UnityCode -Snippet ($Global:ProbeFloorCollapsePrelude + @'
wave.SetWavePause(false);
return "ok";
'@) | Out-Null
}

# 開始・終了の通知を数える（Play を止めるまで購読したままにする）
function Register-FloorCollapseCounters {
    Invoke-UnityCode -Snippet ($Global:ProbeFloorCollapsePrelude + @'
System.AppDomain.CurrentDomain.SetData("ProbeFloorCollapseStarted", 0);
System.AppDomain.CurrentDomain.SetData("ProbeFloorCollapseCompleted", 0);
R3.ObservableSubscribeExtensions.Subscribe(collapse.OnStarted, _ =>
    System.AppDomain.CurrentDomain.SetData("ProbeFloorCollapseStarted", (int)System.AppDomain.CurrentDomain.GetData("ProbeFloorCollapseStarted") + 1));
R3.ObservableSubscribeExtensions.Subscribe(collapse.OnCompleted, _ =>
    System.AppDomain.CurrentDomain.SetData("ProbeFloorCollapseCompleted", (int)System.AppDomain.CurrentDomain.GetData("ProbeFloorCollapseCompleted") + 1));
return "ok";
'@) | Out-Null
}

function ProbeRun {
    Register-FloorCollapseCounters
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
    Assert-ProbeValue -Name '開始の通知（OnStarted）が1回流れる' -Actual $started.started -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '崩壊中はショップが開かない' -Condition (-not $started.shopOpen) | Out-Null
    Assert-ProbeTrue -Name '崩壊中はフォーカス入力が止まる' -Condition (-not $started.focusInput) | Out-Null

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
    Assert-ProbeValue -Name '終了の通知（OnCompleted）が1回流れる' -Actual $done.completed -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '崩壊しきったらショップが開く' -Condition $done.shopOpen | Out-Null

    # --- 3. 次のウェーブで床が戻る ---
    Invoke-NextWave
    Start-Sleep -Milliseconds 300
    $restored = Get-FloorCollapseState
    Assert-ProbeTrue -Name '次のウェーブで床が元に戻る' `
        -Condition ($restored.phase -eq 'Idle' -and $restored.targetVisible -and -not $restored.platesVisible) `
        -Detail "(phase=$($restored.phase) target=$($restored.targetVisible) plates=$($restored.platesVisible))" | Out-Null
    Assert-ProbeTrue -Name '次のウェーブではフォーカス入力が戻り、ショップは閉じている' -Condition ($restored.focusInput -and -not $restored.shopOpen) | Out-Null

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
    Assert-ProbeTrue -Name '打ち切ったときは終了の通知を流さず、ショップの待ちも消える' `
        -Condition ($end.started -eq 2 -and $end.completed -eq 1 -and $end.focusInput -and -not $end.shopOpen) `
        -Detail "(started=$($end.started) completed=$($end.completed) focus=$($end.focusInput) shop=$($end.shopOpen))" | Out-Null

    # 打ち切った後のウェーブクリアでは、待ちが残っていなければ崩壊しきるまでショップは開かない
    Invoke-WaveClear
    Start-Sleep -Milliseconds 300
    $third = Get-FloorCollapseState
    Assert-ProbeTrue -Name '3回目の崩壊中もショップは開かない' -Condition ($third.phase -eq 'Collapsing' -and -not $third.shopOpen) | Out-Null
    Start-Sleep -Seconds ([double]$third.duration + 0.5)
    $thirdDone = Get-FloorCollapseState
    Assert-ProbeTrue -Name '3回目も崩壊しきったらショップが開く' -Condition ($thirdDone.phase -eq 'Completed' -and $thirdDone.shopOpen) | Out-Null
    Invoke-NextWave
}
