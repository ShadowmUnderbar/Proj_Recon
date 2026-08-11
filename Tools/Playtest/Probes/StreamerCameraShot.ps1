#
# ストリーマーモードの配信用カメラ（StreamerCameraView）の演出を数値で検証するプローブ。
#
# 目視スクリーンショットでは「寄って戻った」ことしか分からないため、
# 追従一致・フレーミング距離・注視方向・周回・復帰・プレイヤー視点への非干渉を
# Transformとカメラ設定の実測値で確認する。
#
# 被写体座標とショットのパラメータはC#スニペット側にも同じ値を書いている（下の定数と必ず揃える）。
#

# 被写体3体（C#スニペットの subjects と一致させる）
$script:SubjectPositions = @(
    @{ x = 2.0; y = 0.0; z = 3.0 },
    @{ x = -1.0; y = 0.0; z = 4.0 },
    @{ x = 1.0; y = 0.0; z = 6.0 }
)

# 観測用ショットのパラメータ（C#スニペットの値と一致させる）
$script:ShotHeight = 3.0
$script:ShotFieldOfView = 45.0
$script:ShotBaseDistance = 6.0
$script:ShotFramingMargin = 1.35
$script:ShotMinDistance = 2.0   # StreamerCameraFramingCalculator.MinDistance

function ProbePrepare {
    # 配信カメラはPCモードでは既定で無効なので、観測できるよう一時的に有効化する。
    # Play中にフラグを変えてもViewのSetupは終わっているため、必ずPlay前に行う
    $Global:StreamerProbeOriginal = Invoke-UnityJson -Snippet @'
using UnityEngine;
using UnityEditor;
using App.Common.Data;

var path = "Assets/App/MasterData/StreamerCamera/StreamerModeConfig.asset";
var cfg = AssetDatabase.LoadAssetAtPath<StreamerModeConfig>(path);
if (cfg == null) throw new System.Exception("StreamerModeConfigが見つかりません: " + path);

var original = $"{{\"isEnabled\":{cfg.IsEnabled.ToString().ToLower()},\"vrOnly\":{cfg.VROnly.ToString().ToLower()},\"suppressMirrorView\":{cfg.SuppressMirrorView.ToString().ToLower()}}}";

var s = new SerializedObject(cfg);
s.FindProperty("_isEnabled").boolValue = true;
s.FindProperty("_vrOnly").boolValue = false;            // PCモードでも観測する
s.FindProperty("_suppressMirrorView").boolValue = false; // EditorではXR未初期化なのでミラー抑制は触らない
s.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssets();

return original;
'@
    Write-Host "  元の設定を退避: isEnabled=$($Global:StreamerProbeOriginal.isEnabled) vrOnly=$($Global:StreamerProbeOriginal.vrOnly) suppressMirrorView=$($Global:StreamerProbeOriginal.suppressMirrorView)"
}

function ProbeCleanup {
    $original = $Global:StreamerProbeOriginal
    if ($null -eq $original) { return }

    $snippet = @'
using UnityEngine;
using UnityEditor;
using App.Common.Data;

var path = "Assets/App/MasterData/StreamerCamera/StreamerModeConfig.asset";
var cfg = AssetDatabase.LoadAssetAtPath<StreamerModeConfig>(path);
var s = new SerializedObject(cfg);
s.FindProperty("_isEnabled").boolValue = __IS_ENABLED__;
s.FindProperty("_vrOnly").boolValue = __VR_ONLY__;
s.FindProperty("_suppressMirrorView").boolValue = __SUPPRESS_MIRROR__;
s.ApplyModifiedPropertiesWithoutUndo();
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
return $"isEnabled={cfg.IsEnabled} vrOnly={cfg.VROnly} suppressMirrorView={cfg.SuppressMirrorView}";
'@
    $snippet = $snippet.Replace('__IS_ENABLED__', $original.isEnabled.ToString().ToLower())
    $snippet = $snippet.Replace('__VR_ONLY__', $original.vrOnly.ToString().ToLower())
    $snippet = $snippet.Replace('__SUPPRESS_MIRROR__', $original.suppressMirrorView.ToString().ToLower())

    $restored = Invoke-UnityCode -Snippet $snippet
    Write-Host "  設定を復元: $restored"
}

function ProbeRun {
    # --- 1. 演出前: 配信カメラがプレイヤーカメラと完全一致しているか ---
    $baseline = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<IStreamerCameraView>() as MonoBehaviour;
if (view == null) throw new System.Exception("IStreamerCameraViewを解決できません");

var cam = view.GetComponent<Camera>();
var main = Camera.main;
if (main == null) throw new System.Exception("Camera.mainが取得できません");

return $"{{\"active\":{view.gameObject.activeInHierarchy.ToString().ToLower()},\"camEnabled\":{cam.enabled.ToString().ToLower()},\"isMainCameraTag\":{view.CompareTag("MainCamera").ToString().ToLower()},\"depth\":{cam.depth},\"targetDisplay\":{cam.targetDisplay},\"posDelta\":{Vector3.Distance(view.transform.position, main.transform.position)},\"rotDelta\":{Quaternion.Angle(view.transform.rotation, main.transform.rotation)}}}";
'@

    Assert-ProbeTrue -Name '配信カメラが有効' -Condition ([bool]$baseline.active -and [bool]$baseline.camEnabled) | Out-Null
    Assert-ProbeTrue -Name 'MainCameraタグが付いていない' -Condition (-not [bool]$baseline.isMainCameraTag) `
        -Detail '(付いているとCamera.mainがプレイヤーカメラを指さなくなる)' | Out-Null
    Assert-ProbeTrue -Name 'プレイヤーカメラより手前のDepth' -Condition ([double]$baseline.depth -gt 0) -Detail "(depth=$($baseline.depth))" | Out-Null
    Assert-ProbeValue -Name '追従時の位置差' -Actual ([double]$baseline.posDelta) -Expected 0 -Tolerance 0.0001 | Out-Null
    Assert-ProbeValue -Name '追従時の回転差(度)' -Actual ([double]$baseline.rotDelta) -Expected 0 -Tolerance 0.01 | Out-Null

    # --- 2. 長尺ショットを発火（中間状態を観測するためholdを長くする） ---
    $fired = Invoke-UnityJson -Snippet @'
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var presenter = scope.Container.Resolve<IStreamerCameraPresenter>();
var ds = scope.Container.Resolve<IStreamerCameraDataStore>();

// 中間状態を観測するための長尺ショット（アセット化しないメモリ上のみ）
var shot = ScriptableObject.CreateInstance<StreamerCameraShotData>();
var s = new SerializedObject(shot);
s.FindProperty("_shotId").stringValue = "ProbeLongHold";
s.FindProperty("_shotType").enumValueIndex = (int)StreamerCameraShotType.Orbit;
s.FindProperty("_distance").floatValue = 6f;
s.FindProperty("_height").floatValue = 3f;
s.FindProperty("_yawOffset").floatValue = 30f;
s.FindProperty("_orbitSpeed").floatValue = 20f;
s.FindProperty("_lookAtHeight").floatValue = 1f;
s.FindProperty("_fieldOfView").floatValue = 45f;
s.FindProperty("_useAutoFraming").boolValue = true;
s.FindProperty("_framingMargin").floatValue = 1.35f;
s.FindProperty("_blendInDuration").floatValue = 0.1f;
s.FindProperty("_holdDuration").floatValue = 120f;
s.FindProperty("_blendOutDuration").floatValue = 0.3f;
s.FindProperty("_priority").intValue = 99;
s.FindProperty("_cooldownSeconds").floatValue = 0f;
s.ApplyModifiedPropertiesWithoutUndo();

var subjects = new List<Vector3>
{
    new Vector3(2f, 0f, 3f),
    new Vector3(-1f, 0f, 4f),
    new Vector3(1f, 0f, 6f),
};

var main = Camera.main;
var mainPos = main.transform.position;

var canPlay = ds.CanPlay(shot);
ds.BeginShot(shot);
presenter.PlayShot(new StreamerCameraShotRequest(shot, subjects, "Probe"));

return $"{{\"canPlay\":{canPlay.ToString().ToLower()},\"isPlaying\":{ds.IsPlaying.CurrentValue.ToString().ToLower()},\"mainCamX\":{mainPos.x},\"mainCamY\":{mainPos.y},\"mainCamZ\":{mainPos.z}}}";
'@

    Assert-ProbeTrue -Name 'ショットを再生できる状態' -Condition ([bool]$fired.canPlay) | Out-Null
    Assert-ProbeTrue -Name '再生中フラグが立つ' -Condition ([bool]$fired.isPlaying) | Out-Null

    # ブレンドイン(0.1秒)の完了を待つ
    Start-Sleep -Seconds 2

    # --- 3. 演出中の構図を測る ---
    $mid = Invoke-UnityJson -Snippet (Get-MeasureSnippet)

    $expected = Get-ExpectedFraming -Aspect ([double]$mid.aspect)

    Assert-ProbeTrue -Name '演出中も再生中フラグが立っている' -Condition ([bool]$mid.isPlaying) | Out-Null
    Assert-ProbeValue -Name '視野角がショット設定値' -Actual ([double]$mid.fov) -Expected $script:ShotFieldOfView -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '被写体中心からの高さ' -Actual ([double]$mid.heightAboveCenter) -Expected $script:ShotHeight -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '自動フレーミングの水平距離' -Actual ([double]$mid.horizDistToCenter) -Expected $expected.Distance -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '注視点を向いている(内積)' -Actual ([double]$mid.lookAtDot) -Expected 1.0 -Tolerance 0.0001 | Out-Null

    # 演出中にプレイヤー視点（HMD側）が動かされていないこと
    Assert-ProbeValue -Name 'プレイヤーカメラが不動(X)' -Actual ([double]$mid.mainCamX) -Expected ([double]$fired.mainCamX) -Tolerance 0.0001 | Out-Null
    Assert-ProbeValue -Name 'プレイヤーカメラが不動(Y)' -Actual ([double]$mid.mainCamY) -Expected ([double]$fired.mainCamY) -Tolerance 0.0001 | Out-Null
    Assert-ProbeValue -Name 'プレイヤーカメラが不動(Z)' -Actual ([double]$mid.mainCamZ) -Expected ([double]$fired.mainCamZ) -Tolerance 0.0001 | Out-Null

    # --- 4. 周回しているか（Orbit）→ その後キャンセル ---
    Start-Sleep -Seconds 1
    $orbit = Invoke-UnityJson -Snippet (Get-MeasureSnippet)

    $orbitDelta = [Math]::Sqrt(
        [Math]::Pow([double]$orbit.camX - [double]$mid.camX, 2) +
        [Math]::Pow([double]$orbit.camY - [double]$mid.camY, 2) +
        [Math]::Pow([double]$orbit.camZ - [double]$mid.camZ, 2))
    Assert-ProbeTrue -Name 'Orbitで回り込んでいる' -Condition ($orbitDelta -gt 0.05) -Detail "(移動量 $([Math]::Round($orbitDelta, 3))m)" | Out-Null
    Assert-ProbeValue -Name '周回中も距離を保っている' -Actual ([double]$orbit.horizDistToCenter) -Expected $expected.Distance -Tolerance 0.01 | Out-Null

    Invoke-UnityCode -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
scope.Container.Resolve<IStreamerCameraPresenter>().CancelShot();
return "cancelled";
'@ | Out-Null

    # --- 5. プレイヤー視点へ戻り切ったか ---
    Start-Sleep -Seconds 2
    $restored = Invoke-UnityJson -Snippet (Get-MeasureSnippet)

    Assert-ProbeTrue -Name '復帰後は再生中フラグが下りている' -Condition (-not [bool]$restored.isPlaying) `
        -Detail '(Presenter→UseCase→DataStoreの完了通知が機能している)' | Out-Null
    Assert-ProbeValue -Name '復帰後の位置差' -Actual ([double]$restored.posDelta) -Expected 0 -Tolerance 0.0001 | Out-Null
    Assert-ProbeValue -Name '復帰後の回転差(度)' -Actual ([double]$restored.rotDelta) -Expected 0 -Tolerance 0.01 | Out-Null
}

function Get-ExpectedFraming {
    # StreamerCameraFramingCalculatorと同じ式を独立に計算し、実測値と突き合わせる
    param([Parameter(Mandatory)] [double]$Aspect)

    $count = $script:SubjectPositions.Count
    $cx = ($script:SubjectPositions | Measure-Object -Property x -Sum).Sum / $count
    $cy = ($script:SubjectPositions | Measure-Object -Property y -Sum).Sum / $count
    $cz = ($script:SubjectPositions | Measure-Object -Property z -Sum).Sum / $count

    $radius = 0.0
    foreach ($p in $script:SubjectPositions) {
        $d = [Math]::Sqrt([Math]::Pow($p.x - $cx, 2) + [Math]::Pow($p.y - $cy, 2) + [Math]::Pow($p.z - $cz, 2))
        if ($d -gt $radius) { $radius = $d }
    }

    $verticalTan = [Math]::Tan($script:ShotFieldOfView * [Math]::PI / 180.0 * 0.5)
    $horizontalTan = $verticalTan * $Aspect
    $minTan = [Math]::Min($verticalTan, $horizontalTan)
    $required = $radius / $minTan * $script:ShotFramingMargin

    $distance = [Math]::Max($script:ShotMinDistance, [Math]::Max($script:ShotBaseDistance, $required))

    Write-Host "  期待フレーミング: center=($([Math]::Round($cx,3)), $([Math]::Round($cy,3)), $([Math]::Round($cz,3))) radius=$([Math]::Round($radius,4)) aspect=$Aspect -> distance=$([Math]::Round($distance,4))"

    return @{ CenterX = $cx; CenterY = $cy; CenterZ = $cz; Radius = $radius; Distance = $distance }
}

function Get-MeasureSnippet {
    # 被写体中心は SubjectPositions の重心。C#側にも同じ値を埋め込んでいる
    return @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var ds = scope.Container.Resolve<IStreamerCameraDataStore>();
var view = scope.Container.Resolve<IStreamerCameraView>() as MonoBehaviour;
var cam = view.GetComponent<Camera>();
var main = Camera.main;

var center = new Vector3(0.6666667f, 0f, 4.3333335f);
var lookAt = center + Vector3.up * 1f; // ショットのLookAtHeight=1
var pos = view.transform.position;

var toLookAt = (lookAt - pos).normalized;
var lookAtDot = Vector3.Dot(view.transform.forward, toLookAt);
var horizDist = Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(center.x, 0f, center.z));
var mainPos = main.transform.position;

return $"{{\"isPlaying\":{ds.IsPlaying.CurrentValue.ToString().ToLower()},\"camX\":{pos.x},\"camY\":{pos.y},\"camZ\":{pos.z},\"fov\":{cam.fieldOfView},\"aspect\":{cam.aspect},\"lookAtDot\":{lookAtDot},\"heightAboveCenter\":{pos.y - center.y},\"horizDistToCenter\":{horizDist},\"mainCamX\":{mainPos.x},\"mainCamY\":{mainPos.y},\"mainCamZ\":{mainPos.z},\"posDelta\":{Vector3.Distance(pos, mainPos)},\"rotDelta\":{Quaternion.Angle(view.transform.rotation, main.transform.rotation)}}}";
'@
}
