#
# バトル中のチュートリアルメッセージ（TutorialMessageUseCase / TutorialMessageView）を数値で検証するプローブ。
#
# 目視では「出た・動いた」しか分からないため、
#   1. 配置計算（TutorialMessagePlacement）の純粋ロジック（フェーズ切替・左右ミラー・スナップ・向きのフォールバック）
#   2. 実際の表示（視点正面への追従 → 非利き手の手のひら側へ移動し向きも手に固定 → 差し替え → 非表示）
#   3. 縮小表示（非利き手追従中、手のひらを見ていなければ狭めた幅で表示上の1行目＋「…」へ縮み、
#      読める面が頭を向き、かつ視線が当たったときだけ元の大きさへ戻る）
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
using App.Common.Views;
using App.Common.Data;

var settings = new TutorialMessagePlacementSettings(
    headFollowDuration: 2f,
    headOffset: new Vector3(0f, -0.1f, 1f),
    handOffset: new Vector3(0.2f, 0.3f, 0.1f),
    handRotation: Quaternion.Euler(0f, -90f, -90f),
    facingAngle: 45f,
    facingExitMargin: 10f,
    followSpeed: 0f); // 補間なし＝目標へ即座に置く

var head = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 90f, 0f));
var hand = new Pose(new Vector3(0.5f, 1.0f, 0.5f), Quaternion.Euler(10f, 20f, 30f));

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
var headFacing = placement.IsFacingHead;

// 規定時間を過ぎたら非利き手の脇（左手のときはオフセットそのまま）。
// 経過時間は最初に配置されたフレームの次から数えるため、初回の 1 秒は含まれない
placement.TryUpdate(2.5f, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out var leftPose);
var leftPhase = placement.Phase;
var expectedLeftPos = hand.position + hand.rotation * settings.HandOffset;
var leftPosError = Vector3.Distance(leftPose.position, expectedLeftPos);
// 向きは手に固定（手の回転×手ローカルの回転）。頭の方は向かない
var leftRotError = Quaternion.Angle(leftPose.rotation, hand.rotation * settings.HandRotation);

// 右手のときは x を反転して鏡写し
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Right, hand, true), settings, out var rightPose);
var mirrored = settings.HandOffset; mirrored.x = -mirrored.x;
var expectedRightPos = hand.position + hand.rotation * mirrored;
var rightPosError = Vector3.Distance(rightPose.position, expectedRightPos);

// 鏡写し: 手が無回転なら、右手の前方・上方は左手のものの x を反転した向きになる
var identityHand = new Pose(hand.position, Quaternion.identity);
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, identityHand, true), settings, out var leftIdentity);
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Right, identityHand, true), settings, out var rightIdentity);
var leftForward = leftIdentity.rotation * Vector3.forward; leftForward.x = -leftForward.x;
var leftUp = leftIdentity.rotation * Vector3.up; leftUp.x = -leftUp.x;
var mirrorForwardError = Vector3.Angle(rightIdentity.rotation * Vector3.forward, leftForward);
var mirrorUpError = Vector3.Angle(rightIdentity.rotation * Vector3.up, leftUp);

// 既定の回転では、左手の読める面は手のひらの向こう（手ローカル -x）を向き、文字の上は指先側（+z）
var palmForwardError = Vector3.Angle(leftIdentity.rotation * Vector3.forward, Vector3.left);
var palmUpError = Vector3.Angle(leftIdentity.rotation * Vector3.up, Vector3.forward);

// 手のひらを頭へ向ける（読める面の前方が頭→メッセージの向き）と IsFacingHead、手首を 180 度返すと外れる
var zeroOffset = new TutorialMessagePlacementSettings(2f, settings.HeadOffset, Vector3.zero, settings.HandRotation, 45f, 10f, 0f);
var front = head.position + head.rotation * Vector3.forward * 0.5f;
var palmToHead = Quaternion.LookRotation(front - head.position) * Quaternion.Inverse(settings.HandRotation);
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, new Pose(front, palmToHead), true), zeroOffset, out _);
var palmFacing = placement.IsFacingHead;
var palmAway = Quaternion.AngleAxis(180f, head.rotation * Vector3.up) * palmToHead;
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, new Pose(front, palmAway), true), zeroOffset, out _);
var backFacing = placement.IsFacingHead;
// 向いていない状態からは45度以内で向いている扱いになり、50度では入らない
Pose Tilt(float angle) => new Pose(front, Quaternion.AngleAxis(angle, head.rotation * Vector3.up) * palmToHead);
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, Tilt(50f), true), zeroOffset, out _);
var tilt50Facing = placement.IsFacingHead;
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, Tilt(40f), true), zeroOffset, out _);
var tilt40Facing = placement.IsFacingHead;
// 向いている状態からは余白（10度）ぶん外れにくい: 50度ではまだ向いている、60度で外れる
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, Tilt(50f), true), zeroOffset, out _);
var tilt50Staying = placement.IsFacingHead;
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, Tilt(60f), true), zeroOffset, out _);
var tilt60Facing = placement.IsFacingHead;

// 手が使えない（非VR）間は時間が過ぎても視点の正面に留まる
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, hand, false), settings, out var noHandPose);
var noHandPhase = placement.Phase;
var noHandPosError = Vector3.Distance(noHandPose.position, expectedHeadPos);

// Begin で視点正面フェーズからやり直す
placement.Begin();
placement.TryUpdate(0.1f, new TutorialMessageAnchor(head, HandType.Left, hand, true), settings, out _);
var restartPhase = placement.Phase;

placement.End();
var endPhase = placement.Phase;
var endFacing = placement.IsFacingHead;

return $"{{\"hiddenResult\":{hiddenResult.ToString().ToLower()},\"hiddenPhase\":\"{hiddenPhase}\",\"beginPhase\":\"{beginPhase}\",\"headPhase\":\"{headPhase}\",\"headPosError\":{headPosError},\"headRotError\":{headRotError},\"leftPhase\":\"{leftPhase}\",\"leftPosError\":{leftPosError},\"leftRotError\":{leftRotError},\"rightPosError\":{rightPosError},\"mirrorForwardError\":{mirrorForwardError},\"mirrorUpError\":{mirrorUpError},\"palmForwardError\":{palmForwardError},\"palmUpError\":{palmUpError},\"headFacing\":{headFacing.ToString().ToLower()},\"palmFacing\":{palmFacing.ToString().ToLower()},\"backFacing\":{backFacing.ToString().ToLower()},\"tilt40Facing\":{tilt40Facing.ToString().ToLower()},\"tilt50Facing\":{tilt50Facing.ToString().ToLower()},\"tilt50Staying\":{tilt50Staying.ToString().ToLower()},\"tilt60Facing\":{tilt60Facing.ToString().ToLower()},\"noHandPhase\":\"{noHandPhase}\",\"noHandPosError\":{noHandPosError},\"restartPhase\":\"{restartPhase}\",\"endPhase\":\"{endPhase}\",\"endFacing\":{endFacing.ToString().ToLower()}}}";
'@

    Assert-ProbeTrue -Name '[計算] 非表示中は姿勢を返さない' -Condition (-not [bool]$logic.hiddenResult) `
        -Detail "(phase: $($logic.hiddenPhase))" | Out-Null
    Assert-ProbeTrue -Name '[計算] Begin で視点正面フェーズになる' -Condition ($logic.beginPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeTrue -Name '[計算] 規定時間前は視点正面のまま' -Condition ($logic.headPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeValue -Name '[計算] 視点正面の位置＝頭＋頭ローカルオフセット' -Actual ([double]$logic.headPosError) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '[計算] 視点正面の向き＝頭の向き[deg]' -Actual ([double]$logic.headRotError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeTrue -Name '[計算] 規定時間後は非利き手フェーズになる' -Condition ($logic.leftPhase -eq 'HandFollow') | Out-Null
    Assert-ProbeValue -Name '[計算] 左手の位置＝手＋手ローカルオフセット' -Actual ([double]$logic.leftPosError) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '[計算] 非利き手フェーズの向きは手に固定[deg]' -Actual ([double]$logic.leftRotError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '[計算] 右手のときは x を反転して配置' -Actual ([double]$logic.rightPosError) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '[計算] 右手の前方は左手の鏡写し[deg]' -Actual ([double]$logic.mirrorForwardError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '[計算] 右手の上方は左手の鏡写し[deg]' -Actual ([double]$logic.mirrorUpError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '[計算] 既定の向き: 読める面は手のひらの向こう（-x）[deg]' -Actual ([double]$logic.palmForwardError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '[計算] 既定の向き: 文字の上は指先側（+z）[deg]' -Actual ([double]$logic.palmUpError) -Expected 0 -Tolerance 0.01 | Out-Null
    Assert-ProbeTrue -Name '[計算] 視点正面フェーズは頭の方を向いている' -Condition ([bool]$logic.headFacing) | Out-Null
    Assert-ProbeTrue -Name '[計算] 手のひらを頭へ向けると IsFacingHead' -Condition ([bool]$logic.palmFacing) | Out-Null
    Assert-ProbeTrue -Name '[計算] 手首を返して手の甲側を向けると外れる' -Condition (-not [bool]$logic.backFacing) | Out-Null
    Assert-ProbeTrue -Name '[計算] 判定角度（45度）以内の傾きは向いている扱い' -Condition ([bool]$logic.tilt40Facing -and -not [bool]$logic.tilt50Facing) `
        -Detail "(40度: $($logic.tilt40Facing), 50度: $($logic.tilt50Facing))" | Out-Null
    Assert-ProbeTrue -Name '[計算] 向いている間は余白（10度）ぶん外れにくい' -Condition ([bool]$logic.tilt50Staying -and -not [bool]$logic.tilt60Facing) `
        -Detail "(向いた後の50度: $($logic.tilt50Staying), 60度: $($logic.tilt60Facing))" | Out-Null
    Assert-ProbeTrue -Name '[計算] 手が使えない間は視点正面に留まる' -Condition ($logic.noHandPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeValue -Name '[計算] 手が使えない間の位置＝視点正面' -Actual ([double]$logic.noHandPosError) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '[計算] 再 Begin で視点正面からやり直す' -Condition ($logic.restartPhase -eq 'HeadFollow') | Out-Null
    Assert-ProbeTrue -Name '[計算] End で非表示になる' -Condition ($logic.endPhase -eq 'Hidden' -and -not [bool]$logic.endFacing) | Out-Null

    # --- 1b. 追従の遅延: 頭の移動は即時、向きの変化だけ遅れて追いつく ---
    $lag = Invoke-UnityJson -Snippet @'
using UnityEngine;
using App.Battle.Data;
using App.Battle.Views;
using App.Common.Views;
using App.Common.Data;

var settings = new TutorialMessagePlacementSettings(
    headFollowDuration: 100f,
    headOffset: new Vector3(0f, -0.1f, 1f),
    handOffset: Vector3.zero,
    handRotation: Quaternion.identity,
    facingAngle: 45f,
    facingExitMargin: 0f,
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

    # --- 1c. 縮小・展開の状態（TutorialMessageFold）: 縮小は本文を先に、展開は背景が広がりきってから本文を戻す ---
    $fold = Invoke-UnityJson -Snippet @'
using App.Battle.Views;
using App.Common.Views;

var fold = new TutorialMessageFold();
var initialProgress = fold.Progress;
var initialCollapsed = fold.IsTextCollapsed;

// 縮小を求めた最初のフレームで本文は1行目になり、背景は時間をかけて縮む
fold.Update(0.05f, true, 0.15f);
var collapseStartProgress = fold.Progress;
var collapseStartText = fold.IsTextCollapsed;
fold.Update(0.2f, true, 0.15f);
var collapsedProgress = fold.Progress;

// 展開を求めても、背景が広がりきるまで本文は1行目のまま
fold.Update(0.05f, false, 0.15f);
var expandMidProgress = fold.Progress;
var expandMidText = fold.IsTextCollapsed;
fold.Update(0.2f, false, 0.15f);
var expandedProgress = fold.Progress;
var expandedText = fold.IsTextCollapsed;

// 時間 0 なら即座に切り替わる
fold.Update(0f, true, 0f);
var instantProgress = fold.Progress;

fold.Reset();
return $"{{\"initialProgress\":{initialProgress},\"initialCollapsed\":{initialCollapsed.ToString().ToLower()},\"collapseStartProgress\":{collapseStartProgress},\"collapseStartText\":{collapseStartText.ToString().ToLower()},\"collapsedProgress\":{collapsedProgress},\"expandMidProgress\":{expandMidProgress},\"expandMidText\":{expandMidText.ToString().ToLower()},\"expandedProgress\":{expandedProgress},\"expandedText\":{expandedText.ToString().ToLower()},\"instantProgress\":{instantProgress},\"resetProgress\":{fold.Progress},\"resetText\":{fold.IsTextCollapsed.ToString().ToLower()}}}";
'@

    Assert-ProbeTrue -Name '[縮小] 初期状態は展開' -Condition ([double]$fold.initialProgress -eq 0 -and -not [bool]$fold.initialCollapsed) | Out-Null
    Assert-ProbeTrue -Name '[縮小] 縮小の最初のフレームで本文は1行目になる' -Condition ([bool]$fold.collapseStartText) | Out-Null
    Assert-ProbeValue -Name '[縮小] 背景は時間をかけて縮む（0.05s/0.15s）' -Actual ([double]$fold.collapseStartProgress) -Expected (1.0 / 3.0) -Tolerance 0.001 | Out-Null
    Assert-ProbeValue -Name '[縮小] 時間が経てば縮みきる' -Actual ([double]$fold.collapsedProgress) -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '[縮小] 展開の途中は本文が1行目のまま' -Condition ([bool]$fold.expandMidText) `
        -Detail "(進み具合: $([math]::Round([double]$fold.expandMidProgress, 3)))" | Out-Null
    Assert-ProbeTrue -Name '[縮小] 広がりきったら本文を全文に戻す' -Condition ([double]$fold.expandedProgress -eq 0 -and -not [bool]$fold.expandedText) | Out-Null
    Assert-ProbeValue -Name '[縮小] 時間0なら即座に切り替わる' -Actual ([double]$fold.instantProgress) -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '[縮小] Reset で展開しきった状態へ戻る' -Condition ([double]$fold.resetProgress -eq 0 -and -not [bool]$fold.resetText) | Out-Null

    # --- 2. 初期状態と設定値。ウェーブ1開始時に TutorialWaveConfig の割り当てで自動表示されているので、一旦消して素の状態にする ---
    $setup = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Views;
using App.Common.Views;
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
using App.Common.Interface;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Views;
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
using App.Common.Interface;
using App.Battle.Views;
using App.Common.Views;

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

    # --- 4. 規定時間後 → 非利き手の手のひら側へ移動し、向きも手に固定する ---
    Start-Sleep -Milliseconds ([int]([double]$setup.duration * 1000) + 2000)

    $handFollow = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Views;
using App.Common.Views;
using App.Common.Data;
using App.Common.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var handOffset = (Vector3)typeof(TutorialMessageView).GetField("_handOffset", flags).GetValue(view);
var handEuler = (Vector3)typeof(TutorialMessageView).GetField("_handRotationEuler", flags).GetValue(view);

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

var local = Quaternion.Euler(handEuler);
if (hand == HandType.Right) local = new Quaternion(local.x, -local.y, -local.z, local.w);
var expectedRotation = adjusted * local;

var posError = Vector3.Distance(view.transform.position, expected);
var otherError = Vector3.Distance(view.transform.position, otherExpected);
var rotError = Quaternion.Angle(view.transform.rotation, expectedRotation);

return $"{{\"phase\":\"{view.Phase}\",\"hand\":\"{hand}\",\"posError\":{posError},\"otherHandError\":{otherError},\"rotError\":{rotError}}}";
'@

    if ([bool]$setup.isVr) {
        Assert-ProbeTrue -Name '規定時間後は非利き手フェーズ' -Condition ($handFollow.phase -eq 'HandFollow') `
            -Detail "(非利き手: $($handFollow.hand))" | Out-Null
        Assert-ProbeValue -Name '非利き手の位置（手＋オフセット）[m]' -Actual ([double]$handFollow.posError) -Expected 0 -Tolerance 0.02 | Out-Null
        Assert-ProbeTrue -Name '利き手側には置かれていない' -Condition ([double]$handFollow.otherHandError -gt 0.05) `
            -Detail "(利き手側との距離: $([math]::Round([double]$handFollow.otherHandError, 3))m)" | Out-Null
        Assert-ProbeValue -Name '非利き手フェーズの向き（手＋手ローカル回転）[deg]' -Actual ([double]$handFollow.rotError) -Expected 0 -Tolerance 1.0 | Out-Null
    }
    else {
        Assert-ProbeTrue -Name '非VRでは規定時間後も視点正面のまま' -Condition ($handFollow.phase -eq 'HeadFollow') | Out-Null
    }

    # --- 4b. 縮小表示: 非利き手追従中は手のひらを見ていなければ1行目＋「…」へ縮み、見れば元の大きさへ戻る ---
    # 「手のひらを見ている」＝読める面が頭を向いている（_facingAngle 以内）かつ視線が当たっている。
    # エディタでは HMD もコントローラも動かず、手元のダイアログが頭のすぐ近くに来て判定球の中に入ってしまう。
    # そのため視線は判定の半径・余白角度、向きは _facingAngle（180＝常に向いている、0＝向いていない）を
    # 一時的に書き換えて作り、最後にプレハブの値へ戻す
    $foldSnippet = @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Common.Interface;
using App.Battle.Views;
using App.Common.Views;
using TMPro;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
var body = (TextMeshProUGUI)typeof(TutorialMessageView).GetField("_bodyText", flags).GetValue(view);
var gaze = (GazeTargetView)typeof(TutorialMessageView).GetField("_gazeTarget", flags).GetValue(view);
var expandedSize = (Vector2)typeof(TutorialMessageView).GetField("_expandedSize", flags).GetValue(view);
var expandedBodySize = (Vector2)typeof(TutorialMessageView).GetField("_expandedBodySize", flags).GetValue(view);
var collapsedBodySize = (Vector2)typeof(TutorialMessageView).GetField("_collapsedBodySize", flags).GetValue(view);
var widthRatio = (float)typeof(TutorialMessageView).GetField("_collapsedWidthRatio", flags).GetValue(view);
var originalRadius = (float)typeof(GazeTargetView).GetField("_radius", flags).GetValue(gaze);
var originalEnter = (float)typeof(GazeTargetView).GetField("_enterMarginAngle", flags).GetValue(gaze);
var originalExit = (float)typeof(GazeTargetView).GetField("_exitMarginAngle", flags).GetValue(gaze);
var originalFacing = (float)typeof(TutorialMessageView).GetField("_facingAngle", flags).GetValue(view);

// 負の値は「変更しない」。半径0・余白0なら中心を正確に射抜かない限り当たらない
var radius = __RADIUS__;
var margin = __MARGIN__;
if (radius >= 0f) typeof(GazeTargetView).GetField("_radius", flags).SetValue(gaze, radius);
if (margin >= 0f)
{
    typeof(GazeTargetView).GetField("_enterMarginAngle", flags).SetValue(gaze, margin);
    typeof(GazeTargetView).GetField("_exitMarginAngle", flags).SetValue(gaze, margin);
}
var facing = __FACING__;
if (facing >= 0f) typeof(TutorialMessageView).GetField("_facingAngle", flags).SetValue(view, facing);
var placement = (TutorialMessagePlacement)typeof(TutorialMessageView).GetField("_placement", flags).GetValue(view);

var info = body.textInfo;
var firstLineChars = info.lineCount > 0 ? info.lineInfo[0].visibleCharacterCount : 0;
var visibleLines = 0;
for (var i = 0; i < info.lineCount; i++) if (info.lineInfo[i].visibleCharacterCount > 0) visibleLines++;
var hasEllipsis = false;
for (var i = 0; i < info.characterCount; i++) if (info.characterInfo[i].isVisible && info.characterInfo[i].character == '…') hasEllipsis = true;

var canvasRect = (RectTransform)view.transform;
var gazeCenterOffset = Vector3.Distance(gaze.transform.position, view.transform.position);

return $"{{\"phase\":\"{view.Phase}\",\"isGazed\":{gaze.IsGazed.CurrentValue.ToString().ToLower()},\"width\":{canvasRect.sizeDelta.x},\"height\":{canvasRect.sizeDelta.y},\"bodyWidth\":{body.rectTransform.sizeDelta.x},\"bodyHeight\":{body.rectTransform.sizeDelta.y},\"expandedWidth\":{expandedSize.x},\"expandedHeight\":{expandedSize.y},\"expandedBodyWidth\":{expandedBodySize.x},\"expandedBodyHeight\":{expandedBodySize.y},\"collapsedBodyWidth\":{collapsedBodySize.x},\"collapsedBodyHeight\":{collapsedBodySize.y},\"widthRatio\":{widthRatio},\"firstLineChars\":{firstLineChars},\"fontSize\":{body.fontSize},\"autoSizing\":{body.enableAutoSizing.ToString().ToLower()},\"visibleLines\":{visibleLines},\"hasEllipsis\":{hasEllipsis.ToString().ToLower()},\"gazeCenterOffset\":{gazeCenterOffset},\"originalRadius\":{originalRadius},\"originalEnter\":{originalEnter},\"originalExit\":{originalExit},\"originalFacing\":{originalFacing},\"isFacing\":{placement.IsFacingHead.ToString().ToLower()}}}";
'@

    if ([bool]$setup.isVr) {
        # 見ていない状態にする → 外れ遅延0.5s＋縮小0.15sを待つ
        $original = Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '0f').Replace('__MARGIN__', '0f').Replace('__FACING__', '180f'))
        Start-Sleep -Milliseconds 2000
        $collapsed = Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f'))
        $padding = [double]$collapsed.expandedHeight - [double]$collapsed.expandedBodyHeight
        $paddingX = [double]$collapsed.expandedWidth - [double]$collapsed.expandedBodyWidth

        Assert-ProbeTrue -Name '[縮小] 手元のダイアログを見ていない' -Condition (-not [bool]$collapsed.isGazed) `
            -Detail "(phase: $($collapsed.phase))" | Out-Null
        Assert-ProbeValue -Name '[縮小] 見ていなければ高さが1行目＋余白まで縮む[px]' -Actual ([double]$collapsed.height) `
            -Expected ([double]$collapsed.collapsedBodyHeight + $padding) -Tolerance 0.5 | Out-Null
        Assert-ProbeTrue -Name '[縮小] 縮小時の高さは展開時より小さい' -Condition ([double]$collapsed.height -lt [double]$collapsed.expandedHeight) `
            -Detail "($([math]::Round([double]$collapsed.height, 1))px / 展開 $($collapsed.expandedHeight)px)" | Out-Null
        Assert-ProbeValue -Name '[縮小] 本文の折り返し幅は展開時×割合[px]' -Actual ([double]$collapsed.bodyWidth) `
            -Expected ([double]$collapsed.expandedBodyWidth * [double]$collapsed.widthRatio) -Tolerance 0.01 | Out-Null
        Assert-ProbeValue -Name '[縮小] ダイアログの横幅も本文幅＋余白まで縮む[px]' -Actual ([double]$collapsed.width) `
            -Expected ([double]$collapsed.collapsedBodyWidth + $paddingX) -Tolerance 0.5 | Out-Null
        Assert-ProbeTrue -Name '[縮小] 本文は表示上の1行だけ' -Condition ([int]$collapsed.visibleLines -eq 1) `
            -Detail "(表示行数: $($collapsed.visibleLines))" | Out-Null
        Assert-ProbeTrue -Name '[縮小] 続きがあるので末尾が「…」' -Condition ([bool]$collapsed.hasEllipsis) | Out-Null
        Assert-ProbeTrue -Name '[縮小] 文字サイズは固定（自動サイズ無効）' -Condition (-not [bool]$collapsed.autoSizing) `
            -Detail "(文字サイズ: $($collapsed.fontSize))" | Out-Null
        Assert-ProbeValue -Name '[縮小] 判定の中心はダイアログの中心[m]' -Actual ([double]$collapsed.gazeCenterOffset) -Expected 0 -Tolerance 0.001 | Out-Null

        # 判定半径を広げて「見た」状態にする（頭が判定球の中に入る）。入り遅延0.15s＋展開0.15sを待つ
        Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '100f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f')) | Out-Null
        Start-Sleep -Milliseconds 1500
        $expanded = Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f'))

        Assert-ProbeTrue -Name '[縮小] 見ると IsGazed になる' -Condition ([bool]$expanded.isGazed -and [bool]$expanded.isFacing) `
            -Detail "(IsGazed: $($expanded.isGazed), IsFacingHead: $($expanded.isFacing))" | Out-Null
        Assert-ProbeValue -Name '[縮小] 見ると元の横幅へ戻る[px]' -Actual ([double]$expanded.width) -Expected ([double]$expanded.expandedWidth) -Tolerance 0.01 | Out-Null
        Assert-ProbeValue -Name '[縮小] 見ると本文の折り返し幅も元に戻る[px]' -Actual ([double]$expanded.bodyWidth) -Expected ([double]$expanded.expandedBodyWidth) -Tolerance 0.01 | Out-Null
        # 1行目の文字数は折り返し幅の割合とほぼ同じだけ減る（「…」の置き換えと禁則で数文字ずれるため幅を持たせる）
        $charRatio = [double]$collapsed.firstLineChars / [math]::Max(1, [double]$expanded.firstLineChars)
        Assert-ProbeTrue -Name '[縮小] 1行目の文字数がおおむね割合ぶんに減る' `
            -Condition ([math]::Abs($charRatio - [double]$collapsed.widthRatio) -le 0.15) `
            -Detail "(縮小 $($collapsed.firstLineChars)文字 / 展開 $($expanded.firstLineChars)文字 = $([math]::Round($charRatio, 2)), 割合 $($collapsed.widthRatio))" | Out-Null
        Assert-ProbeValue -Name '[縮小] 見ると元の高さへ戻る[px]' -Actual ([double]$expanded.height) -Expected ([double]$expanded.expandedHeight) -Tolerance 0.01 | Out-Null
        Assert-ProbeValue -Name '[縮小] 見ると本文の枠も元に戻る[px]' -Actual ([double]$expanded.bodyHeight) -Expected ([double]$expanded.expandedBodyHeight) -Tolerance 0.01 | Out-Null
        Assert-ProbeTrue -Name '[縮小] 見ると全文が表示される' -Condition ([int]$expanded.visibleLines -gt 1 -and -not [bool]$expanded.hasEllipsis) `
            -Detail "(表示行数: $($expanded.visibleLines))" | Out-Null
        Assert-ProbeValue -Name '[縮小] 縮小・展開で文字サイズが変わらない' -Actual ([double]$expanded.fontSize) -Expected ([double]$collapsed.fontSize) -Tolerance 0.001 | Out-Null

        # 視線は当たったまま、手のひらを向けていない状態にする → 縮小0.15s後に縮む
        Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '0f')) | Out-Null
        Start-Sleep -Milliseconds 1000
        $notFacing = Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f'))

        Assert-ProbeTrue -Name '[手のひら] 視線は当たっているが手のひらを向けていない' -Condition ([bool]$notFacing.isGazed -and -not [bool]$notFacing.isFacing) `
            -Detail "(IsGazed: $($notFacing.isGazed), IsFacingHead: $($notFacing.isFacing))" | Out-Null
        Assert-ProbeValue -Name '[手のひら] 手のひらを向けていなければ縮む[px]' -Actual ([double]$notFacing.height) -Expected ([double]$collapsed.height) -Tolerance 0.5 | Out-Null

        # 手のひらを向け直す → 展開する
        Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '180f')) | Out-Null
        Start-Sleep -Milliseconds 1000
        $refacing = Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f'))
        Assert-ProbeValue -Name '[手のひら] 手のひらを向けて見ると展開する[px]' -Actual ([double]$refacing.height) -Expected ([double]$refacing.expandedHeight) -Tolerance 0.01 | Out-Null

        # 再び見ていない状態にする → 外れ遅延0.5s＋縮小0.15s後に再び縮む
        Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '0f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f')) | Out-Null
        Start-Sleep -Milliseconds 2000
        $recollapsed = Invoke-UnityJson -Snippet ($foldSnippet.Replace('__RADIUS__', '-1f').Replace('__MARGIN__', '-1f').Replace('__FACING__', '-1f'))

        Assert-ProbeValue -Name '[縮小] 視線を外すと再び縮む[px]' -Actual ([double]$recollapsed.height) -Expected ([double]$collapsed.height) -Tolerance 0.5 | Out-Null
        Assert-ProbeValue -Name '[縮小] 視線を外すと横幅も再び縮む[px]' -Actual ([double]$recollapsed.width) -Expected ([double]$collapsed.width) -Tolerance 0.5 | Out-Null

        # 書き換えた判定の設定を元の値へ戻す（再生中の以降の挙動に影響させない）
        $restore = @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Common.Interface;
using App.Battle.Views;
using App.Common.Views;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var view = scope.Container.Resolve<ITutorialMessageView>() as TutorialMessageView;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var gaze = (GazeTargetView)typeof(TutorialMessageView).GetField("_gazeTarget", flags).GetValue(view);
typeof(GazeTargetView).GetField("_radius", flags).SetValue(gaze, __R__f);
typeof(GazeTargetView).GetField("_enterMarginAngle", flags).SetValue(gaze, __E__f);
typeof(GazeTargetView).GetField("_exitMarginAngle", flags).SetValue(gaze, __X__f);
typeof(TutorialMessageView).GetField("_facingAngle", flags).SetValue(view, __F__f);
return "restored";
'@
        Invoke-UnityCode -Snippet ($restore.Replace('__R__', $original.originalRadius).Replace('__E__', $original.originalEnter).Replace('__X__', $original.originalExit).Replace('__F__', $original.originalFacing)) | Out-Null
    }

    # --- 5. 差し替え → 視点正面からやり直す。非表示 → ルートが消える ---
    $reshow = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Common.Interface;
using App.Battle.UseCase;
using App.Common.Data;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
scope.Container.Resolve<ITutorialMessageUseCase>().Show(TutorialType.Wave1);
var view = scope.Container.Resolve<ITutorialMessageView>();
var size = ((UnityEngine.RectTransform)((UnityEngine.Component)view).transform).sizeDelta;
var expandedSize = (UnityEngine.Vector2)typeof(App.Common.Views.TutorialMessageView)
    .GetField("_expandedSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(view);

return $"{{\"phase\":\"{view.Phase}\",\"width\":{size.x},\"height\":{size.y},\"expandedWidth\":{expandedSize.x},\"expandedHeight\":{expandedSize.y}}}";
'@

    Assert-ProbeTrue -Name '再 Show で視点正面フェーズへ戻る' -Condition ($reshow.phase -eq 'HeadFollow') | Out-Null
    Assert-ProbeValue -Name '[縮小] 再 Show で展開した大きさから始まる[px]' -Actual ([double]$reshow.height) -Expected ([double]$reshow.expandedHeight) -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '[縮小] 再 Show で展開した横幅から始まる[px]' -Actual ([double]$reshow.width) -Expected ([double]$reshow.expandedWidth) -Tolerance 0.01 | Out-Null

    $hidden = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Common.Interface;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Views;

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
