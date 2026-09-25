#
# プレイヤー死亡演出（GameOverUseCase の死亡シーケンス）を数値で検証するプローブ。
#
# 目視では「何か倒れた」ことしか分からないため、
# ヒットストップ → 死亡アニメ → 余韻 の順に進み、アニメが終わるまで
# ゲームオーバー画面が出ないことを、Animatorの再生位置と経過時間で確認する。
#

function ProbePrepare {
    # デバッグ用「開始時アップグレード」のバリアが致死ダメージを吸収すると死ねないため、
    # プローブの間だけ空にして ProbeCleanup で戻す
    $Global:ProbeSavedStartUpgradeIds = Invoke-UnityCode -Snippet @'
using UnityEditor;
using App.Common.Data;

var saved = EditorPrefs.GetString(DebugConfig.StartUpgradeIdsKey, string.Empty);
EditorPrefs.SetString(DebugConfig.StartUpgradeIdsKey, string.Empty);

return saved;
'@

    Write-Host "開始時アップグレードを退避しました: [$Global:ProbeSavedStartUpgradeIds]"
}

function ProbeCleanup {
    $saved = $Global:ProbeSavedStartUpgradeIds

    # -replaceはパターン側が正規表現なので、バックスラッシュ1文字は '\\' と書く
    $escaped = $saved -replace '\\', '\\' -replace '"', '\"'

    Invoke-UnityCode -Snippet @"
using UnityEditor;
using App.Common.Data;

EditorPrefs.SetString(DebugConfig.StartUpgradeIdsKey, "$escaped");

return "restored";
"@ | Out-Null

    Write-Host "開始時アップグレードを戻しました: [$saved]"
}

function ProbeRun {
    # --- 1. 演出の設定値とAnimatorの構成を確認する ---
    $setup = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var config = scope.Container.Resolve<PlayerDeathConfig>();

var playerView = scope.Container.Resolve<IBattlePlayerView>() as MonoBehaviour;
if (playerView == null) throw new System.Exception("IBattlePlayerViewをMonoBehaviourとして解決できません");

var animator = playerView.GetComponentInChildren<Animator>(true);
if (animator == null) throw new System.Exception("プレイヤーのAnimatorが見つかりません");

var deathLayerIndex = animator.GetLayerIndex("Death");
var clipLength = 0f;
if (deathLayerIndex >= 0)
{
    foreach (var clip in animator.runtimeAnimatorController.animationClips)
    {
        if (clip.name.Contains("Death")) { clipLength = clip.length; break; }
    }
}

return $"{{\"hitStop\":{config.HitStopDuration},\"timeout\":{config.AnimationTimeout},\"delay\":{config.PostAnimationDelay},\"deathLayerIndex\":{deathLayerIndex},\"deathLayerWeight\":{(deathLayerIndex >= 0 ? animator.GetLayerWeight(deathLayerIndex) : -1f)},\"clipLength\":{clipLength}}}";
'@

    Assert-ProbeTrue -Name '死亡アニメ用レイヤーがある' -Condition ([int]$setup.deathLayerIndex -ge 0) `
        -Detail "(index: $($setup.deathLayerIndex))" | Out-Null
    Assert-ProbeValue -Name '死亡前のレイヤーウェイト' -Actual ([double]$setup.deathLayerWeight) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '死亡クリップの長さを取得できる' -Condition ([double]$setup.clipLength -gt 0) `
        -Detail "(実測: $($setup.clipLength)秒)" | Out-Null

    # --- 2. HPを0にして演出を開始する ---
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $killed = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();
var gameState = scope.Container.Resolve<IGameStateDataStore>();
var freeze = scope.Container.Resolve<IFreezeDataStore>();

// バリアは1発を丸ごと吸収するため、HPが0になるまで繰り返し当てる
for (var i = 0; i < 10 && player.Health.Value > 0f; i++)
{
    player.TakeDamage(99999f);
}

// ヒットストップは死亡シーケンスの最初のawaitより手前で掛かるので、この時点で既に立っている。
// 別の呼び出しで見るとuloopの往復（数百ms）の間に明けてしまい観測できない
var isFreezing = freeze.IsFreezing.CurrentValue;

var panel = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel");
var panelShown = panel != null && panel.activeInHierarchy;

return $"{{\"health\":{player.Health.Value},\"isGameOver\":{gameState.IsGameOver.CurrentValue.ToString().ToLower()},\"panelShown\":{panelShown.ToString().ToLower()},\"isFreezing\":{isFreezing.ToString().ToLower()}}}";
'@

    Assert-ProbeValue -Name 'HPが0になっている' -Actual ([double]$killed.health) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name 'ゲームオーバー状態になる' -Condition ([bool]$killed.isGameOver) | Out-Null
    Assert-ProbeTrue -Name '死亡直後はまだ画面が出ていない' -Condition (-not [bool]$killed.panelShown) `
        -Detail '(演出の前に出てしまうと待つ意味がない)' | Out-Null
    Assert-ProbeTrue -Name '死亡と同時にヒットストップが掛かる' -Condition ([bool]$killed.isFreezing) `
        -Detail "(設定: $($setup.hitStop)秒)" | Out-Null

    # --- 3. ヒットストップが明け、死亡アニメが始まるまで待つ ---
    Start-Sleep -Milliseconds 400

    $playing = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var playerView = scope.Container.Resolve<IBattlePlayerView>() as MonoBehaviour;
var animator = playerView.GetComponentInChildren<Animator>(true);
var deathLayerIndex = animator.GetLayerIndex("Death");
var state = animator.GetCurrentAnimatorStateInfo(deathLayerIndex);

var panel = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel");
var panelShown = panel != null && panel.activeInHierarchy;

return $"{{\"weight\":{animator.GetLayerWeight(deathLayerIndex)},\"isDeathState\":{state.IsName("Death").ToString().ToLower()},\"normalizedTime\":{state.normalizedTime},\"panelShown\":{panelShown.ToString().ToLower()},\"finished\":{playerView.GetComponent<App.Battle.Views.BattlePlayerView>().IsDeathAnimationFinished.ToString().ToLower()}}}";
'@

    Assert-ProbeValue -Name '再生中の死亡レイヤーウェイト' -Actual ([double]$playing.weight) -Expected 1 | Out-Null
    Assert-ProbeTrue -Name '死亡ステートが再生されている' -Condition ([bool]$playing.isDeathState) | Out-Null
    Assert-ProbeTrue -Name 'アニメ再生中はまだ画面が出ていない' -Condition (-not [bool]$playing.panelShown) `
        -Detail "(再生位置: $([math]::Round([double]$playing.normalizedTime, 2)))" | Out-Null

    # --- 4. アニメ終了＋余韻のあとに画面が出る ---
    $shown = $false
    for ($i = 0; $i -lt 30; $i++) {
        $state = Invoke-UnityJson -Snippet @'
using UnityEngine;

var panel = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel");
var panelShown = panel != null && panel.activeInHierarchy;

return $"{{\"panelShown\":{panelShown.ToString().ToLower()}}}";
'@
        if ([bool]$state.panelShown) { $shown = $true; break }
        Start-Sleep -Milliseconds 200
    }
    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.TotalSeconds

    Assert-ProbeTrue -Name '演出後にゲームオーバー画面が出る' -Condition $shown `
        -Detail "(経過: $([math]::Round($elapsed, 2))秒)" | Out-Null

    # ヒットストップ＋クリップ長＋余韻を下回らないこと（＝待たずに出ていないこと）。
    # 観測のポーリング間隔ぶん上振れするため上限は緩めに見る
    $expected = [double]$setup.hitStop + [double]$setup.clipLength + [double]$setup.delay
    Assert-ProbeTrue -Name '演出時間ぶん待ってから出ている' -Condition ($elapsed -ge $expected * 0.8) `
        -Detail "(期待: $([math]::Round($expected, 2))秒以上 / 実測: $([math]::Round($elapsed, 2))秒)" | Out-Null
    Assert-ProbeTrue -Name '演出が長引きすぎていない' -Condition ($elapsed -le $expected + 2.0) `
        -Detail "(実測: $([math]::Round($elapsed, 2))秒)" | Out-Null

    # --- 5. リスタートで倒れた姿勢が元に戻る ---
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'
        'target-path' = 'BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/RestartButton'
        'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 1000

    $afterRestart = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var playerView = scope.Container.Resolve<IBattlePlayerView>() as MonoBehaviour;
var animator = playerView.GetComponentInChildren<Animator>(true);
var deathLayerIndex = animator.GetLayerIndex("Death");
var runStart = scope.Container.Resolve<IRunStartDataStore>();

return $"{{\"weight\":{animator.GetLayerWeight(deathLayerIndex)},\"isSelecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()}}}";
'@

    Assert-ProbeValue -Name 'リスタート後の死亡レイヤーウェイト' -Actual ([double]$afterRestart.weight) -Expected 0 `
        | Out-Null
    Assert-ProbeTrue -Name 'リスタートでビルド選択へ戻る' -Condition ([bool]$afterRestart.isSelecting) | Out-Null
}
