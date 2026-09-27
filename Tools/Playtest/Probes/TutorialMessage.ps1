#
# バトル中のチュートリアルメッセージ（TutorialMessageUseCase / TutorialMessageView）を数値で検証するプローブ。
#
# 目視では「出た・動いた」しか分からないため、
#   1. 配置計算（TutorialMessagePlacement）の純粋ロジック（フェーズ切替・左右ミラー・スナップ・向きのフォールバック）
#   2. 実際の表示（視点正面への追従 → 非利き手の脇への移動 → 常に頭を向く → 差し替え → 非表示）
# を Transform と設定値の実測で確認する。
#
# HMD の無いエディタではカメラもコントローラも動かないため、「追従している」ことは
# 「目標位置（頭／手の姿勢＋オフセット）に収束している」ことで見る。
#

function ProbePrepare {
    # 開いているシーンを切り替えず（未保存の変更を失わないよう）、再生開始時だけ Battle シーンを使う
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

    # 非利き手追従はVRモードでしか動かない（非VRはコントローラ姿勢が更新されない）ため、
    # DebugConfig が static readonly で読む前＝Play前にVRモードを強制し、ProbeCleanup で戻す
    $Global:TutorialMessageProbePreviousVrMode = Invoke-UnityCode -Snippet @'
var before = UnityEditor.EditorPrefs.GetBool("VRMode", false);
UnityEditor.EditorPrefs.SetBool("VRMode", true);
return before.ToString().ToLower();
'@
    Write-Host "VRモードを一時的に有効化しました（元の値: $Global:TutorialMessageProbePreviousVrMode）"
}

function ProbeCleanup {
    if ($null -ne $Global:TutorialMessageProbePreviousVrMode) {
        $snippet = @'
UnityEditor.EditorPrefs.SetBool("VRMode", __PREVIOUS__);
return UnityEditor.EditorPrefs.GetBool("VRMode", false).ToString().ToLower();
'@.Replace('__PREVIOUS__', $Global:TutorialMessageProbePreviousVrMode)
        Invoke-UnityCode -Snippet $snippet | Out-Null
        Write-Host "VRモードを元に戻しました（$Global:TutorialMessageProbePreviousVrMode）"
        $Global:TutorialMessageProbePreviousVrMode = $null
    }

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

function ProbeRun {
    # --- 1. 配置計算の純粋ロジック（plain C#、再生状態に依存しない） ---
    $logic = Invoke-UnityJson -Snippet @'
using UnityEngine;
using App.Battle.Data;
using App.Battle.Views;
using App.Common.Data;

var settings = new TutorialMessagePlacementSettings(
    headFollowDuration: 2f,
    headOffset: new Vector3(0f, -0.1f, 1f),
    handOffset: new Vector3(-0.2f, 0.3f, 0.1f),
    followSpeed: 0f); // 補間なし＝目標へ即座に置く

var head = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 90f, 0f));
var hand = new Pose(new Vector3(0.5f, 1.0f, 0.5f), Quaternion.identity);

var placement = new TutorialMessagePlacement();
var hiddenResult = placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out _);
var hiddenPhase = placement.Phase;

placement.Begin();
var beginPhase = placement.Phase;

// 規定時間前は視点の正面。位置＝頭＋頭ローカルのオフセット、向き＝頭と同じ
placement.TryUpdate(1f, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out var headPose);
var headPhase = placement.Phase;
var expectedHeadPos = head.position + head.rotation * settings.HeadOffset;
var headPosError = Vector3.Distance(headPose.position, expectedHeadPos);
var headRotError = Quaternion.Angle(headPose.rotation, head.rotation);

// 規定時間を過ぎたら非利き手の脇（左手のときはオフセットそのまま）。
// 経過時間は最初に配置されたフレームの次から数えるため、初回の 1 秒は含まれない
placement.TryUpdate(2.5f, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out var leftPose);
var leftPhase = placement.Phase;
var expectedLeftPos = hand.position + hand.rotation * settings.HandOffset;
var leftPosError = Vector3.Distance(leftPose.position, expectedLeftPos);
var faceError = Vector3.Angle(leftPose.rotation * Vector3.forward, leftPose.position - head.position);

// 右手のときは x を反転して鏡写し
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Right, hand, true), settings, out var rightPose);
var mirrored = settings.HandOffset; mirrored.x = -mirrored.x;
var expectedRightPos = hand.position + hand.rotation * mirrored;
var rightPosError = Vector3.Distance(rightPose.position, expectedRightPos);

// 手が使えない（非VR）間は時間が過ぎても視点の正面に留まる
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, hand, false), settings, out var noHandPose);
var noHandPhase = placement.Phase;
var noHandPosError = Vector3.Distance(noHandPose.position, expectedHeadPos);

// 真下に来て水平成分が消えたときは頭の向きへフォールバックし、壊れた回転を返さない
var belowHand = new Pose(head.position + Vector3.down * 0.5f, Quaternion.identity);
var zeroOffset = new TutorialMessagePlacementSettings(2f, settings.HeadOffset, Vector3.zero, 0f);
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, belowHand, true), zeroOffset, out var belowPose);
var belowRotError = Quaternion.Angle(belowPose.rotation, head.rotation);

// Begin で視点正面フェーズからやり直す
placement.Begin();
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out _);
var restartPhase = placement.Phase;

placement.End();
var endPhase = placement.Phase;

return $"{{\"hiddenResult\":{hiddenResult.ToString().ToLower()},\"hiddenPhase\":\"{hiddenPhase}\",\"beginPhase\":\"{beginPhase}\",\"headPhase\":\"{headPhase}\",\"headPosError\":{headPosError},\"headRotError\":{headRotError},\"leftPhase\":\"{leftPhase}\",\"leftPosError\":{leftPosError},\"faceError\":{faceError},\"rightPosError\":{rightPosError},\"noHandPhase\":\"{noHandPhase}\",\"noHandPosError\":{noHandPosError},\"belowRotError\":{belowRotError},\"restartPhase\":\"{restartPhase}\",\"endPhase\":\"{endPhase}\"}}";
'@

    Assert-ProbeTrue -Name '[計算] 非表示中は姿勢を返さない' -Condition (-not [bool]$logic.hiddenResult) `
        -Detail "(phase: $($logic.hiddenPhase))" | Out-Null
    Assert-ProbeTrue -Name '[計算] Begin で視点正面フェーズになる' -Condition ($logic.beginPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeTrue -Name '[計算] 規定時間前は視点正面のまま' -Condition ($logic.headPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeValue -Name '[計算] 視点正面の位置＝頭＋頭ローカルオフセット' -Actual ([double]$logic.headPosError) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '[計算] 視点正面の向き＝頭の向き[deg]' -Actual ([double]$logic.headRotError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeTrue -Name '[計算] 規定時間後は非利き手フェーズになる' -Condition ($logic.leftPhase -eq 'HandFollow') | Out-Null
    Assert-ProbeValue -Name '[計算] 左手の位置＝手＋手ローカルオフセット' -Actual ([double]$logic.leftPosError) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '[計算] 非利き手フェーズは頭の方を向く[deg]' -Actual ([double]$logic.faceError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '[計算] 右手のときは x を反転して配置' -Actual ([double]$logic.rightPosError) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '[計算] 手が使えない間は視点正面に留まる' -Condition ($logic.noHandPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeValue -Name '[計算] 手が使えない間の位置＝視点正面' -Actual ([double]$logic.noHandPosError) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '[計算] 真下では頭の向きへフォールバック[deg]' -Actual ([double]$logic.belowRotError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeTrue -Name '[計算] 再 Begin で視点正面からやり直す' -Condition ($logic.restartPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeTrue -Name '[計算] End で非表示になる' -Condition ($logic.endPhase -eq 'Hidden') | Out-Null

    # --- 1b. 追従の遅延: 頭の移動は即時、向きの変化だけ遅れて追いつく ---
    $lag = Invoke-UnityJson -Snippet @'
using UnityEngine;
using App.Battle.Data;
using App.Battle.Views;
using App.Common.Data;

var settings = new TutorialMessagePlacementSettings(
    headFollowDuration: 100f,
    headOffset: new Vector3(0f, -0.1f, 1f),
    handOffset: Vector3.zero,
    followSpeed: 6f);
var dt = 1f / 90f;
var hand = new Pose(Vector3.zero, Quaternion.identity);

var placement = new TutorialMessagePlacement();
placement.Begin();
var head = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
placement.TryUpdate(dt, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out var start);

// 頭が向きを変えずに 1m 移動 → 同じフレームで 1m 付いてくる（遅延なし）
head = new Pose(head.position + new Vector3(1f, 0f, 0f), head.rotation);
placement.TryUpdate(dt, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out var moved);
var moveError = Vector3.Distance(moved.position, start.position + new Vector3(1f, 0f, 0f));

// 頭が 90 度回る → 同じフレームでは目標に届かず、向きも遅れている
head = new Pose(head.position, Quaternion.Euler(0f, 90f, 0f));
placement.TryUpdate(dt, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out var turned);
var turnTarget = head.position + head.rotation * settings.HeadOffset;
var turnGap = Vector3.Distance(turned.position, turnTarget);
var turnRotGap = Quaternion.Angle(turned.rotation, head.rotation);

// 時間が経てば追いつく
Pose settled = turned;
for (var i = 0; i < 600; i++)
{
    placement.TryUpdate(dt, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out settled);
}
var settledGap = Vector3.Distance(settled.position, turnTarget);
var settledRotGap = Quaternion.Angle(settled.rotation, head.rotation);

return $"{{\"moveError\":{moveError},\"turnGap\":{turnGap},\"turnRotGap\":{turnRotGap},\"settledGap\":{settledGap},\"settledRotGap\":{settledRotGap}}}";
'@

    Assert-ProbeValue -Name '[計算] 頭の移動には遅延なく付いてくる[m]' -Actual ([double]$lag.moveError) -Expected 0 -Tolerance 0.001 | Out-Null
    Assert-ProbeTrue -Name '[計算] 首を回した直後は位置が遅れている' -Condition ([double]$lag.turnGap -gt 0.5) `
        -Detail "(目標との距離: $([math]::Round([double]$lag.turnGap, 3))m)" | Out-Null
    Assert-ProbeTrue -Name '[計算] 首を回した直後は向きが遅れている' -Condition ([double]$lag.turnRotGap -gt 45) `
        -Detail "(目標との角度: $([math]::Round([double]$lag.turnRotGap, 1))deg)" | Out-Null
    Assert-ProbeValue -Name '[計算] 時間が経てば位置が追いつく[m]' -Actual ([double]$lag.settledGap) -Expected 0 -Tolerance 0.001 | Out-Null
    Assert-ProbeValue -Name '[計算] 時間が経てば向きが追いつく[deg]' -Actual ([double]$lag.settledRotGap) -Expected 0 -Tolerance 0.1 | Out-Null

    # --- 2. 初期状態と設定値。ウェーブ1開始時に TutorialWaveConfig の割り当てで自動表示されているので、一旦消して素の状態にする ---
    $setup = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Views;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
if (view == null) throw new System.Exception("ITutorialMessageView を TutorialMessageView として解決できません");

var autoShownPhase = view.Phase.ToString();
scope.Container.Resolve<ITutorialMessageUseCase>().Hide();

var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var root = (GameObject)typeof(TutorialMessageView).GetField("_root", flags).GetValue(view);
var duration = (float)typeof(TutorialMessageView).GetField("_headFollowDuration", flags).GetValue(view);
var setting = scope.Container.Resolve<IPlayerSettingDataStore>();

return $"{{\"autoShownPhase\":\"{autoShownPhase}\",\"phase\":\"{view.Phase}\",\"rootActive\":{root.activeInHierarchy.ToString().ToLower()},\"duration\":{duration},\"nonDominant\":\"{setting.NonDominantHand}\",\"isVr\":{DebugConfig.IsVRMode.ToString().ToLower()}}}";
'@

    Write-Host "ウェーブ1開始時の自動表示: $($setup.autoShownPhase)（未閲覧なら表示、閲覧済みなら Hidden。詳細は TutorialWave プローブ）"
    Assert-ProbeTrue -Name 'Hide で非表示になる（初期化）' -Condition ($setup.phase -eq 'Hidden' -and -not [bool]$setup.rootActive) `
        -Detail "(phase: $($setup.phase), 非利き手: $($setup.nonDominant), VR: $($setup.isVr))" | Out-Null

    if (-not [bool]$setup.isVr) {
        Write-Host '非VRモードのため、非利き手追従の実測は省略します（視点正面に留まる仕様）'
    }

    # --- 3. 表示 → 直後は視点の正面に追従 ---
    $shown = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Data;
using TMPro;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
scope.Container.Resolve<ITutorialMessageUseCase>().Show(TutorialType.Wave1);

var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var body = (TextMeshProUGUI)typeof(TutorialMessageView).GetField("_bodyText", flags).GetValue(view);

return $"{{\"phase\":\"{view.Phase}\",\"textLength\":{body.text.Length},\"text\":\"{body.text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ")}\"}}";
'@

    Assert-ProbeTrue -Name 'Show 直後は視点正面フェーズ' -Condition ($shown.phase -eq 'HeadFollow') | Out-Null
    Assert-ProbeTrue -Name '本文がローカライズから入る' -Condition ([int]$shown.textLength -gt 0 -and $shown.text -ne '$Wave1') `
        -Detail "(本文: $($shown.text))" | Out-Null

    Start-Sleep -Milliseconds 500

    $headFollow = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Views;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var root = (GameObject)typeof(TutorialMessageView).GetField("_root", flags).GetValue(view);
var headOffset = (Vector3)typeof(TutorialMessageView).GetField("_headOffset", flags).GetValue(view);

var cam = Camera.main.transform;
var expected = cam.position + cam.rotation * headOffset;
var posError = Vector3.Distance(view.transform.position, expected);
var rotError = Quaternion.Angle(view.transform.rotation, cam.rotation);

return $"{{\"phase\":\"{view.Phase}\",\"rootActive\":{root.activeInHierarchy.ToString().ToLower()},\"posError\":{posError},\"rotError\":{rotError}}}";
'@

    Assert-ProbeTrue -Name '表示中はルートがアクティブ' -Condition ([bool]$headFollow.rootActive) | Out-Null
    Assert-ProbeTrue -Name '表示直後は視点正面フェーズのまま' -Condition ($headFollow.phase -eq 'HeadFollow') | Out-Null
    Assert-ProbeValue -Name '視点正面の位置（カメラ＋オフセット）[m]' -Actual ([double]$headFollow.posError) -Expected 0 -Tolerance 0.02 | Out-Null
    Assert-ProbeValue -Name '視点正面の向き（カメラと同じ）[deg]' -Actual ([double]$headFollow.rotError) -Expected 0 -Tolerance 1.0 | Out-Null

    # --- 4. 規定時間後 → 非利き手の脇へ移動し、頭の方を向く ---
    Start-Sleep -Milliseconds ([int]([double]$setup.duration * 1000) + 2000)

    $handFollow = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Views;
using App.Common.Data;
using App.Common.Interface;
using App.Common.Views;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var handOffset = (Vector3)typeof(TutorialMessageView).GetField("_handOffset", flags).GetValue(view);

var setting = scope.Container.Resolve<IPlayerSettingDataStore>();
var control = scope.Container.Resolve<IPlayerControlPresenter>();
var hand = setting.NonDominantHand;
var handPose = hand == HandType.Left ? control.LeftHandPose.Value : control.RightHandPose.Value;
var otherPose = hand == HandType.Left ? control.RightHandPose.Value : control.LeftHandPose.Value;

var offset = handOffset;
if (hand == HandType.Right) offset.x = -offset.x;
var adjusted = handPose.rotation * PlatformHandRotation.PointingAdjustment;
var expected = handPose.position + adjusted * offset;

var otherOffset = handOffset;
if (hand == HandType.Left) otherOffset.x = -otherOffset.x;
var otherExpected = otherPose.position + otherPose.rotation * PlatformHandRotation.PointingAdjustment * otherOffset;

var cam = Camera.main.transform;
var posError = Vector3.Distance(view.transform.position, expected);
var otherError = Vector3.Distance(view.transform.position, otherExpected);
var faceError = Vector3.Angle(view.transform.forward, view.transform.position - cam.position);

return $"{{\"phase\":\"{view.Phase}\",\"hand\":\"{hand}\",\"posError\":{posError},\"otherHandError\":{otherError},\"faceError\":{faceError}}}";
'@

    if ([bool]$setup.isVr) {
        Assert-ProbeTrue -Name '規定時間後は非利き手フェーズ' -Condition ($handFollow.phase -eq 'HandFollow') `
            -Detail "(非利き手: $($handFollow.hand))" | Out-Null
        Assert-ProbeValue -Name '非利き手の位置（手＋オフセット）[m]' -Actual ([double]$handFollow.posError) -Expected 0 -Tolerance 0.02 | Out-Null
        Assert-ProbeTrue -Name '利き手側には置かれていない' -Condition ([double]$handFollow.otherHandError -gt 0.05) `
            -Detail "(利き手側との距離: $([math]::Round([double]$handFollow.otherHandError, 3))m)" | Out-Null
        Assert-ProbeValue -Name '非利き手フェーズは頭の方を向く[deg]' -Actual ([double]$handFollow.faceError) -Expected 0 -Tolerance 1.0 | Out-Null
    }
    else {
        Assert-ProbeTrue -Name '非VRでは規定時間後も視点正面のまま' -Condition ($handFollow.phase -eq 'HeadFollow') | Out-Null
    }

    # --- 5. 差し替え → 視点正面からやり直す。非表示 → ルートが消える ---
    $reshow = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.UseCase;
using App.Common.Data;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
scope.Container.Resolve<ITutorialMessageUseCase>().Show(TutorialType.Wave1);
var view = scope.Container.Resolve<ITutorialMessageView>();

return $"{{\"phase\":\"{view.Phase}\"}}";
'@

    Assert-ProbeTrue -Name '再 Show で視点正面フェーズへ戻る' -Condition ($reshow.phase -eq 'HeadFollow') | Out-Null

    $hidden = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.UseCase;
using App.Battle.Views;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
scope.Container.Resolve<ITutorialMessageUseCase>().Hide();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var root = (GameObject)typeof(TutorialMessageView).GetField("_root", flags).GetValue(view);

return $"{{\"phase\":\"{view.Phase}\",\"rootActive\":{root.activeInHierarchy.ToString().ToLower()}}}";
'@

    Assert-ProbeTrue -Name 'Hide で非表示になる' -Condition ($hidden.phase -eq 'Hidden' -and -not [bool]$hidden.rootActive) `
        -Detail "(phase: $($hidden.phase), rootActive: $($hidden.rootActive))" | Out-Null
}
