#
# ゲームオーバーからのリスタート（GameOverUseCase / RunResetUseCase）を数値で検証するプローブ。
#
# 目視ではUIが切り替わったことしか分からないため、
# ラン状態（ウェーブ・ポイント・HP・所持アップグレード・場の敵）が初期化され、
# ビルド選択（RunStartView）へ戻ってそのまま次のランを始められることを実測値で確認する。
#

# ゲームオーバー画面のリスタートボタン
$Global:ProbeGameOverRestartButtonPath = 'BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/RestartButton'

function ProbePrepare {
    # デバッグ用「開始時アップグレード」が入っていると、バリアやHP強化のぶんだけ
    # リスタート後の期待値がぶれる（実際にバリアが致死ダメージを吸って検証が通らなかった）。
    # プローブの間だけ空にし、ProbeCleanupで元へ戻す
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

    # 退避した値をC#のソースへそのまま埋めるため、文字列リテラルを壊す文字をエスケープする。
    # 失敗するとEditorPrefsが戻らず、ユーザーのデバッグ設定を壊したまま終わってしまう
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
    # --- 1. リセット対象のDataStoreがVContainerから集まっている ---
    $resettables = Invoke-UnityJson -Snippet @'
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();

// IRunResettableの実装をまとめて解決できないとリスタートが素通りするため、ここで実数を見る
var resettables = scope.Container.Resolve<IReadOnlyList<IRunResettable>>();
var names = new List<string>();
foreach (var resettable in resettables)
{
    names.Add(resettable.GetType().Name);
}

return $"{{\"count\":{resettables.Count},\"names\":\"{string.Join(",", names)}\"}}";
'@

    Assert-ProbeTrue -Name 'IRunResettableが収集できている' -Condition ([int]$resettables.count -ge 15) `
        -Detail "(実測: $($resettables.count)件 / $($resettables.names))" | Out-Null

    # --- 2. ランを進めた状態を作ってからHPを0にする ---
    $before = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var point = scope.Container.Resolve<IPointDataStore>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var enemyDataStore = scope.Container.Resolve<IEnemyDataStore>();
var upgradeDatabase = scope.Container.Resolve<UpgradeDatabase>();

// ウェーブを進め、ポイント・アップグレード・場の敵を持った「やり込んだ状態」にする
wave.AdvanceWave();
wave.AdvanceWave();
wave.AddElapsedTime(12f);
wave.IncrementKillCount();
point.Add(500);

// 付与副作用は通さない（バリアが張られるとHPを0にできないため）
var master = upgradeDatabase.UpgradeMasterData[0];
session.AddUpgrade(master);

if (!enemyDataStore.TryGetRandomEnemyMasterData(EnemyRankType.Common, UnlockCoreSkillType.First, out var enemyMaster))
{
    throw new System.Exception("Commonランクの敵マスターデータを取得できません");
}
enemyDataStore.AddEnemyData(enemyMaster, new Pose(new Vector3(0f, 0f, 6f), Quaternion.identity));

// プレイヤーを原点から動かしておき、リスタートで戻ることを確かめる
player.Position.Value = new Vector3(7f, 0f, -4f);

var maxHealth = player.MaxHealth.Value;

// 被弾と同じTakeDamage経路でHPを0にしてゲームオーバーを起こす。
// バリアは1発を丸ごと吸収する仕様なので、HPが0になるまで繰り返し当てる
for (var i = 0; i < 10 && player.Health.Value > 0f; i++)
{
    player.TakeDamage(99999f);
}

return $"{{\"wave\":{wave.CurrentWave.CurrentValue},\"point\":{point.CurrentPoint.CurrentValue},\"upgrades\":{session.AppliedUpgrades.Count},\"enemies\":{enemyDataStore.Enemies.Count},\"health\":{player.Health.Value},\"maxHealth\":{maxHealth}}}";
'@

    Assert-ProbeValue -Name 'ゲームオーバー直前のHP' -Actual ([double]$before.health) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name 'ゲームオーバー直前はウェーブが進んでいる' -Condition ([int]$before.wave -ge 3) `
        -Detail "(実測: Wave$($before.wave))" | Out-Null

    # ゲームオーバー画面の表示（UI生成・ハンドレイ切替）を待つ
    Start-Sleep -Milliseconds 800

    $gameOver = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var gameState = scope.Container.Resolve<IGameStateDataStore>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();

// パス形式のGameObject.Findは非アクティブなオブジェクトも返すため、表示はactiveInHierarchyで見る
var panel = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel");
var restartButton = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel/RestartButton");

var panelShown = panel != null && panel.activeInHierarchy;
var restartButtonShown = restartButton != null && restartButton.activeInHierarchy;

return $"{{\"isGameOver\":{gameState.IsGameOver.CurrentValue.ToString().ToLower()},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"panelShown\":{panelShown.ToString().ToLower()},\"restartButtonShown\":{restartButtonShown.ToString().ToLower()}}}";
'@

    Assert-ProbeTrue -Name 'HP0でゲームオーバーになる' -Condition ([bool]$gameOver.isGameOver) | Out-Null
    Assert-ProbeTrue -Name 'ゲームオーバーでウェーブが止まる' -Condition ([bool]$gameOver.isWavePause) | Out-Null
    Assert-ProbeTrue -Name 'ゲームオーバー画面が表示される' -Condition ([bool]$gameOver.panelShown) | Out-Null
    Assert-ProbeTrue -Name 'リスタートボタンが表示される' -Condition ([bool]$gameOver.restartButtonShown) `
        -Detail '(プレハブのExitButtonからのリネームが反映されているか)' | Out-Null

    # --- 3. 保存ボタンを押しても画面は閉じない（保存とリスタートを分けている） ---
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestGameOverSlotButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 500

    $afterSave = Invoke-UnityJson -Snippet @'
using UnityEngine;

var panel = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel");
var panelShown = panel != null && panel.activeInHierarchy;

return $"{{\"panelShown\":{panelShown.ToString().ToLower()}}}";
'@

    Assert-ProbeTrue -Name 'スロット保存してもゲームオーバー画面は開いたまま' -Condition ([bool]$afterSave.panelShown) `
        -Detail '(保存先を選び直してからリスタートできる)' | Out-Null

    # --- 4. リスタートボタンでラン状態が初期化される ---
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:ProbeGameOverRestartButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 1000

    $after = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Battle.Views;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var point = scope.Container.Resolve<IPointDataStore>();
var player = scope.Container.Resolve<IPlayerStateDataStore>();
var session = scope.Container.Resolve<IUpgradeSessionDataStore>();
var enemyDataStore = scope.Container.Resolve<IEnemyDataStore>();
var gameState = scope.Container.Resolve<IGameStateDataStore>();
var runStart = scope.Container.Resolve<IRunStartDataStore>();
var barrier = scope.Container.Resolve<IPlayerBarrierDataStore>();
var dodge = scope.Container.Resolve<IPlayerDodgeParameterDataStore>();

var enemyViews = UnityEngine.Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None).Length;
var gameOverPanel = GameObject.Find("BattleLifetimeScope/GameOverView(Clone)/GameOverCanvas/Panel");
var runStartPanel = GameObject.Find("BattleLifetimeScope/RunStartView(Clone)/RunStartCanvas/Panel");
var gameOverShown = gameOverPanel != null && gameOverPanel.activeInHierarchy;
var runStartShown = runStartPanel != null && runStartPanel.activeInHierarchy;

return $"{{\"wave\":{wave.CurrentWave.CurrentValue},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"kill\":{wave.KillCount.CurrentValue},\"point\":{point.CurrentPoint.CurrentValue},\"upgrades\":{session.AppliedUpgrades.Count},\"enemies\":{enemyDataStore.Enemies.Count},\"enemyViews\":{enemyViews},\"health\":{player.Health.Value},\"maxHealth\":{player.MaxHealth.Value},\"baseHealth\":{BasePlayerParameter.Health},\"posX\":{player.Position.Value.x},\"posZ\":{player.Position.Value.z},\"isGameOver\":{gameState.IsGameOver.CurrentValue.ToString().ToLower()},\"isSelecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"barrier\":{barrier.CurrentBarrier.CurrentValue},\"dodgeCount\":{dodge.DodgeCount.Value},\"gameOverShown\":{gameOverShown.ToString().ToLower()},\"runStartShown\":{runStartShown.ToString().ToLower()}}}";
'@

    Assert-ProbeValue -Name 'リスタート後のウェーブ' -Actual ([double]$after.wave) -Expected 1 | Out-Null
    Assert-ProbeValue -Name 'リスタート後のウェーブ経過時間' -Actual ([double]$after.elapsed) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後の撃破数' -Actual ([double]$after.kill) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後の所持ポイント' -Actual ([double]$after.point) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後の所持アップグレード数' -Actual ([double]$after.upgrades) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後に残る敵データ' -Actual ([double]$after.enemies) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後に残る敵オブジェクト' -Actual ([double]$after.enemyViews) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後の最大HP' -Actual ([double]$after.maxHealth) -Expected ([double]$after.baseHealth) | Out-Null
    Assert-ProbeValue -Name 'リスタート後の現在HP' -Actual ([double]$after.health) -Expected ([double]$after.baseHealth) | Out-Null
    Assert-ProbeValue -Name 'リスタート後のバリア残量' -Actual ([double]$after.barrier) -Expected 0 | Out-Null
    Assert-ProbeValue -Name 'リスタート後の回避回数' -Actual ([double]$after.dodgeCount) -Expected 2 | Out-Null
    Assert-ProbeTrue -Name 'リスタートでプレイヤーが原点へ戻る' `
        -Condition ([math]::Abs([double]$after.posX) -lt 0.01 -and [math]::Abs([double]$after.posZ) -lt 0.01) `
        -Detail "(実測: ($($after.posX), $($after.posZ)))" | Out-Null
    Assert-ProbeTrue -Name 'リスタートでゲームオーバー状態が解除される' -Condition (-not [bool]$after.isGameOver) | Out-Null
    Assert-ProbeTrue -Name 'リスタートでゲームオーバー画面が閉じる' -Condition (-not [bool]$after.gameOverShown) | Out-Null
    Assert-ProbeTrue -Name 'リスタートでビルド選択へ戻る' -Condition ([bool]$after.isSelecting) | Out-Null
    Assert-ProbeTrue -Name 'ビルド選択UIが表示される' -Condition ([bool]$after.runStartShown) | Out-Null
    Assert-ProbeTrue -Name 'ビルド選択中はゲームが止まっている' -Condition ([bool]$after.isWavePause) | Out-Null

    # --- 5. 戻ったビルド選択からそのまま次のランを始められる ---
    Invoke-Uloop -Command 'simulate-mouse-ui' -Params @{
        action = 'Click'; 'target-path' = $Global:PlaytestRunStartButtonPath; 'bypass-raycast' = 'true'
    } | Out-Null
    Start-Sleep -Milliseconds 800

    $restarted = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var wave = scope.Container.Resolve<IWaveManagerDataStore>();
var runStart = scope.Container.Resolve<IRunStartDataStore>();

return $"{{\"isSelecting\":{runStart.IsSelecting.CurrentValue.ToString().ToLower()},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()},\"wave\":{wave.CurrentWave.CurrentValue}}}";
'@

    Assert-ProbeTrue -Name '2回目のランを開始できる' -Condition (-not [bool]$restarted.isSelecting) | Out-Null
    Assert-ProbeTrue -Name '2回目のランでポーズが解除される' -Condition (-not [bool]$restarted.isWavePause) | Out-Null
    Assert-ProbeValue -Name '2回目のランはウェーブ1から' -Actual ([double]$restarted.wave) -Expected 1 | Out-Null
}
