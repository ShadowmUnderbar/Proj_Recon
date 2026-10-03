#
# オーバークロック（OverclockDataStore / OverclockUseCase）を数値で検証するプローブ。
#
# 1. 獲得: 回避中の被弾無効化1回ごとに Value1 秒溜まり、しきい値（3秒）を「超えた」瞬間に発動する。
#    発動中は溜まらず、ストック秒数ぶんで自動終了する
# 2. 発動中: 敵の停止・敵弾の停止・自弾は飛ぶ・トレイル/レイは消えない・視点は発動位置に留まる・
#    被弾はダメージだけ溜まる・ウェーブ経過時間とスポーン周期は進まない
# 3. 終了時: 溜めたダメージを1回で受ける・視点が本体へ戻る・敵弾とトレイルが元へ戻る
# 4. ウェーブ間ポーズに入ると、残り時間があっても終わってダメージを受ける
#
# 発動は数秒で自然に明けるため、長く観測したい段階は AddStock を直接呼んで長めに発動させる。
#

function ProbePrepare {
    # デバッグ用「開始時アップグレード」のバリア等がダメージ量の実測をぶらすため、プローブの間だけ空にする
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
    # --- 1. 獲得と自動発動 ---
    $gain = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.Views;
using App.Common.Data.Database;

var c = LifetimeScope.Find<BattleLifetimeScope>().Container;
var overclock = c.Resolve<IOverclockDataStore>();
var dodge = c.Resolve<IPlayerDodgeParameterDataStore>();
var session = c.Resolve<IUpgradeSessionDataStore>();
var player = c.Resolve<IPlayerStateDataStore>();
var db = c.Resolve<UpgradeDatabase>();
var playerView = c.Resolve<IBattlePlayerView>() as MonoBehaviour;
var receiver = playerView.GetComponentInChildren<PlayerDamageReceiverView>();

// 回避の直線移動中にする（このスニペット内は同期実行なので、移動が終わる前に被弾を流せる）
var pos = player.Position.Value;
dodge.StartDodge(pos, pos + new Vector3(0f, 0f, 0.5f));
var isDodging = dodge.IsDodging.CurrentValue;
var healthBefore = player.Health.Value;

// 敵弾の被弾を実際の経路（PlayerHitUseCase）へ流す。回避中なので無効化される
var projectileId = 1;
System.Action blocked = () => receiver.OnHit(1f, 999, pos, out _, isProjectile: true, projectileId: projectileId++);

// 未所持では溜まらない
blocked();
var stockWithoutUpgrade = overclock.StockSeconds.CurrentValue;

if (!db.TryGetUpgradeMasterData("113", out var master)) throw new System.Exception("Overclock(id=113) がDBにありません");
session.AddUpgrade(master);

var s1 = 0f; var s2 = 0f; var s3 = 0f;
blocked(); s1 = overclock.StockSeconds.CurrentValue;
blocked(); s2 = overclock.StockSeconds.CurrentValue;
blocked(); s3 = overclock.StockSeconds.CurrentValue;
var activeAtThreshold = overclock.IsActive.CurrentValue;

blocked();
var activeOver = overclock.IsActive.CurrentValue;
var remaining = overclock.RemainingTime;
var stockAfterActivate = overclock.StockSeconds.CurrentValue;

// 発動中は溜まらない
blocked();
var stockDuringActive = overclock.StockSeconds.CurrentValue;
var remainingAfterExtra = overclock.RemainingTime;

return $"{{\"isDodging\":{isDodging.ToString().ToLower()},\"healthDelta\":{healthBefore - player.Health.Value},\"stockWithoutUpgrade\":{stockWithoutUpgrade},\"value1\":{master.Value1.value},\"s1\":{s1},\"s2\":{s2},\"s3\":{s3},\"activeAtThreshold\":{activeAtThreshold.ToString().ToLower()},\"activeOver\":{activeOver.ToString().ToLower()},\"remaining\":{remaining},\"stockAfterActivate\":{stockAfterActivate},\"stockDuringActive\":{stockDuringActive},\"remainingAfterExtra\":{remainingAfterExtra}}}";
'@

    Assert-ProbeTrue -Name '回避中の状態を作れている' -Condition ([bool]$gain.isDodging) | Out-Null
    Assert-ProbeValue -Name '回避中の被弾はHPを減らさない' -Actual ([double]$gain.healthDelta) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '未所持では獲得しない' -Actual ([double]$gain.stockWithoutUpgrade) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '無効化1回目でValue1秒溜まる' -Actual ([double]$gain.s1) -Expected ([double]$gain.value1) | Out-Null
    Assert-ProbeValue -Name '無効化3回目で3秒' -Actual ([double]$gain.s3) -Expected (3 * [double]$gain.value1) | Out-Null
    Assert-ProbeTrue -Name 'ちょうど3秒では発動しない' -Condition (-not [bool]$gain.activeAtThreshold) | Out-Null
    Assert-ProbeTrue -Name '3秒を超えると発動する' -Condition ([bool]$gain.activeOver) | Out-Null
    Assert-ProbeValue -Name '発動時間はストック秒数' -Actual ([double]$gain.remaining) -Expected (4 * [double]$gain.value1) -Tolerance 0.05 | Out-Null
    Assert-ProbeValue -Name '発動でストックが0になる' -Actual ([double]$gain.stockAfterActivate) -Expected 0 | Out-Null
    Assert-ProbeValue -Name '発動中は獲得しない' -Actual ([double]$gain.stockDuringActive) -Expected 0 | Out-Null
    Assert-ProbeTrue -Name '発動中の獲得で延長しない' -Condition ([double]$gain.remainingAfterExtra -le [double]$gain.remaining) | Out-Null

    # ストック秒数（4秒）ぶんで自然に終わる
    Start-Sleep -Milliseconds ([int](4 * [double]$gain.value1 * 1000 + 800))
    $natural = Invoke-UnityJson -Snippet @'
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var overclock = LifetimeScope.Find<BattleLifetimeScope>().Container.Resolve<IOverclockDataStore>();
return $"{{\"active\":{overclock.IsActive.CurrentValue.ToString().ToLower()}}}";
'@
    Assert-ProbeTrue -Name 'ストック秒数の経過で自然に終わる' -Condition (-not [bool]$natural.active) | Out-Null

    # --- 2. 長めに発動させて、発動中の状態を観測する ---
    $start = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.Views;
using App.Battle.Views.Enemy.Bullet;
using App.Battle.Data;
using App.Common.Data;
using App.Framework.Utilities;

var scope = LifetimeScope.Find<BattleLifetimeScope>();
var c = scope.Container;
var overclock = c.Resolve<IOverclockDataStore>();
var player = c.Resolve<IPlayerStateDataStore>();
var wave = c.Resolve<IWaveManagerDataStore>();
var spawnCycle = c.Resolve<IEnemyRandomSpawnCycleDataStore>();
var enemyStore = c.Resolve<IEnemyStoreView>();
var tracer = c.Resolve<ITracerFreezeState>();
var playerView = c.Resolve<IBattlePlayerView>() as MonoBehaviour;
var pin = playerView.GetComponentInChildren<PlayerCameraPinView>();
if (pin == null) throw new System.Exception("PlayerCameraPinView が見つかりません");

var flags = BindingFlags.Instance | BindingFlags.NonPublic;
var trailField = typeof(BaseBulletView).GetField("_trailRenderer", flags);

// 敵弾に見立てた弾（撃ち手が敵Id）を発動前に出しておく。プレイヤーから離れた高所を横へ飛ばす
var prefab = (PlayerBulletView)typeof(BattleLifetimeScope).GetField("_playerBulletView", flags).GetValue(scope);
var basePos = player.Position.Value + new Vector3(0f, 8f, 0f);
var enemyBullet = UnityEngine.Object.Instantiate(prefab);
enemyBullet.name = "ProbeEnemyBullet";
enemyBullet.Spawn(12345, new Pose(basePos, Quaternion.LookRotation(Vector3.right)), new BulletData { Speed = 4f }, -1);
var originalTrailTime = ((TrailRenderer)trailField.GetValue(enemyBullet)).time;

var defaultCameraLocal = pin.transform.localPosition;

overclock.AddStock(12f);

// 発動中に撃った自弾（DI経由で生成）
var factory = c.Resolve<ISimpleObjectFactory<IBulletView>>();
var playerBullet = factory.Instantiate(null);
var playerBulletMb = (MonoBehaviour)playerBullet;
playerBulletMb.name = "ProbePlayerBullet";
playerBullet.Spawn(PlayerConstants.PlayerId, new Pose(basePos + new Vector3(0f, 2f, 0f), Quaternion.LookRotation(Vector3.right)), new BulletData { Speed = 4f }, -1);

var pinnedCamera = pin.transform.position;

// 本体を動かす（視点は留まるはず）
player.Position.Value += new Vector3(6f, 0f, 0f);

// 被弾はダメージだけ溜まる
var healthBefore = player.Health.Value;
var receiver = playerView.GetComponentInChildren<PlayerDamageReceiverView>();
receiver.OnHit(10f, 999, player.Position.Value, out _);
receiver.OnHit(10f, 999, player.Position.Value, out _);
var healthAfterHits = player.Health.Value;

var enemyPause = (bool)typeof(App.Battle.Views.EnemyStoreView).GetField("_isPause", flags).GetValue(enemyStore);
var cycle = (float)spawnCycle.GetType().GetField("_commonSpawnCycle", flags).GetValue(spawnCycle);

return $"{{\"active\":{overclock.IsActive.CurrentValue.ToString().ToLower()},\"enemyPause\":{enemyPause.ToString().ToLower()},\"tracerFreezing\":{tracer.IsFreezing.ToString().ToLower()},\"tracerOverclock\":{tracer.IsOverclock.ToString().ToLower()},\"originalTrailTime\":{originalTrailTime},\"enemyBulletX\":{enemyBullet.transform.position.x},\"playerBulletX\":{playerBulletMb.transform.position.x},\"pinnedX\":{pinnedCamera.x},\"pinnedY\":{pinnedCamera.y},\"pinnedZ\":{pinnedCamera.z},\"defaultLocalX\":{defaultCameraLocal.x},\"defaultLocalY\":{defaultCameraLocal.y},\"defaultLocalZ\":{defaultCameraLocal.z},\"healthBefore\":{healthBefore},\"healthAfterHits\":{healthAfterHits},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"cycle\":{cycle}}}";
'@

    Assert-ProbeTrue -Name '発動している' -Condition ([bool]$start.active) | Out-Null
    Assert-ProbeTrue -Name '敵が停止する' -Condition ([bool]$start.enemyPause) | Out-Null
    Assert-ProbeTrue -Name '即着弾のレイを止める' -Condition ([bool]$start.tracerFreezing -and [bool]$start.tracerOverclock) | Out-Null
    Assert-ProbeValue -Name '発動中の被弾でHPは減らない' -Actual ([double]$start.healthAfterHits) -Expected ([double]$start.healthBefore) | Out-Null

    Start-Sleep -Milliseconds 1500

    $during = Invoke-UnityJson -Snippet @'
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.Views;
using App.Battle.Views.Enemy.Bullet;

var c = LifetimeScope.Find<BattleLifetimeScope>().Container;
var overclock = c.Resolve<IOverclockDataStore>();
var player = c.Resolve<IPlayerStateDataStore>();
var wave = c.Resolve<IWaveManagerDataStore>();
var spawnCycle = c.Resolve<IEnemyRandomSpawnCycleDataStore>();
var playerView = c.Resolve<IBattlePlayerView>() as MonoBehaviour;
var pin = playerView.GetComponentInChildren<PlayerCameraPinView>();

var flags = BindingFlags.Instance | BindingFlags.NonPublic;
var trailField = typeof(BaseBulletView).GetField("_trailRenderer", flags);
var enemyBullet = GameObject.Find("ProbeEnemyBullet");
var playerBullet = GameObject.Find("ProbePlayerBullet");
var enemyTrail = enemyBullet != null ? ((TrailRenderer)trailField.GetValue(enemyBullet.GetComponent<BaseBulletView>())) : null;
var playerTrail = playerBullet != null ? ((TrailRenderer)trailField.GetValue(playerBullet.GetComponent<BaseBulletView>())) : null;
var cycle = (float)spawnCycle.GetType().GetField("_commonSpawnCycle", flags).GetValue(spawnCycle);

return $"{{\"active\":{overclock.IsActive.CurrentValue.ToString().ToLower()},\"enemyBulletExists\":{(enemyBullet != null).ToString().ToLower()},\"playerBulletExists\":{(playerBullet != null).ToString().ToLower()},\"enemyBulletX\":{(enemyBullet != null ? enemyBullet.transform.position.x : 0f)},\"playerBulletX\":{(playerBullet != null ? playerBullet.transform.position.x : 0f)},\"enemyTrailTime\":{(enemyTrail != null ? enemyTrail.time : -1f)},\"playerTrailTime\":{(playerTrail != null ? playerTrail.time : -1f)},\"playerTrailPoints\":{(playerTrail != null ? playerTrail.positionCount : -1)},\"cameraX\":{pin.transform.position.x},\"cameraY\":{pin.transform.position.y},\"cameraZ\":{pin.transform.position.z},\"playerRootX\":{playerView.transform.position.x},\"health\":{player.Health.Value},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"cycle\":{cycle}}}";
'@

    Assert-ProbeTrue -Name '観測中も発動が続いている' -Condition ([bool]$during.active) | Out-Null
    Assert-ProbeTrue -Name '敵弾が消えない' -Condition ([bool]$during.enemyBulletExists) | Out-Null
    Assert-ProbeValue -Name '敵弾がその場に止まる' -Actual ([double]$during.enemyBulletX) -Expected ([double]$start.enemyBulletX) -Tolerance 0.01 | Out-Null
    Assert-ProbeTrue -Name '自弾は飛び続ける' -Condition ([double]$during.playerBulletX -gt ([double]$start.playerBulletX + 3)) `
        -Detail "(開始 $($start.playerBulletX) → $($during.playerBulletX))" | Out-Null
    Assert-ProbeTrue -Name '敵弾のトレイルを残す' -Condition ([double]$during.enemyTrailTime -gt 1000) | Out-Null
    Assert-ProbeTrue -Name '自弾のトレイルを残す' -Condition ([double]$during.playerTrailTime -gt 1000) `
        -Detail "(点数: $($during.playerTrailPoints))" | Out-Null
    Assert-ProbeValue -Name '視点X（発動位置に留まる）' -Actual ([double]$during.cameraX) -Expected ([double]$start.pinnedX) -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '視点Z（発動位置に留まる）' -Actual ([double]$during.cameraZ) -Expected ([double]$start.pinnedZ) -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '本体は移動している' -Actual ([double]$during.playerRootX - [double]$start.pinnedX + [double]$start.defaultLocalX) -Expected 6 -Tolerance 0.01 | Out-Null
    Assert-ProbeValue -Name '発動中はHPが変わらない' -Actual ([double]$during.health) -Expected ([double]$start.healthBefore) | Out-Null
    Assert-ProbeValue -Name 'ウェーブ経過時間が進まない' -Actual ([double]$during.elapsed) -Expected ([double]$start.elapsed) -Tolerance 0.001 | Out-Null
    Assert-ProbeValue -Name 'スポーン周期が進まない' -Actual ([double]$during.cycle) -Expected ([double]$start.cycle) -Tolerance 0.001 | Out-Null

    # --- 3. 終了時の処理 ---
    $end = Invoke-UnityJson -Snippet @'
using System.Reflection;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.Views;
using App.Battle.Views.Enemy.Bullet;

var c = LifetimeScope.Find<BattleLifetimeScope>().Container;
var overclock = c.Resolve<IOverclockDataStore>();
var player = c.Resolve<IPlayerStateDataStore>();
var buff = c.Resolve<IBuffStateDataStore>();
var enemyStore = c.Resolve<IEnemyStoreView>();
var tracer = c.Resolve<ITracerFreezeState>();
var playerView = c.Resolve<IBattlePlayerView>() as MonoBehaviour;
var pin = playerView.GetComponentInChildren<PlayerCameraPinView>();

var damagedCount = 0;
var damagedTotal = 0f;
var sub = player.OnDamaged.Subscribe(d => { damagedCount++; damagedTotal += d; });

var healthBefore = player.Health.Value;
// 軽減率はダメージ適用前の状態で読む（被弾条件のバフが終了時の被弾で変わるため）
var multiply = buff.CalcDamageTakenMultiply();
overclock.ForceEnd();
sub.Dispose();

var flags = BindingFlags.Instance | BindingFlags.NonPublic;
var trailField = typeof(BaseBulletView).GetField("_trailRenderer", flags);
var enemyBullet = GameObject.Find("ProbeEnemyBullet");
var enemyTrail = enemyBullet != null ? ((TrailRenderer)trailField.GetValue(enemyBullet.GetComponent<BaseBulletView>())) : null;
var enemyPause = (bool)typeof(EnemyStoreView).GetField("_isPause", flags).GetValue(enemyStore);
var local = pin.transform.localPosition;

return $"{{\"active\":{overclock.IsActive.CurrentValue.ToString().ToLower()},\"healthBefore\":{healthBefore},\"healthAfter\":{player.Health.Value},\"multiply\":{multiply},\"damagedCount\":{damagedCount},\"damagedTotal\":{damagedTotal},\"enemyPause\":{enemyPause.ToString().ToLower()},\"tracerFreezing\":{tracer.IsFreezing.ToString().ToLower()},\"enemyTrailTime\":{(enemyTrail != null ? enemyTrail.time : -1f)},\"enemyBulletX\":{(enemyBullet != null ? enemyBullet.transform.position.x : 0f)},\"localX\":{local.x},\"localY\":{local.y},\"localZ\":{local.z}}}";
'@

    $expectedDamage = [math]::Max(1, 20 * [double]$end.multiply)
    Assert-ProbeTrue -Name '終了している' -Condition (-not [bool]$end.active) | Out-Null
    Assert-ProbeValue -Name '溜めたダメージを1回で受ける（回数）' -Actual ([double]$end.damagedCount) -Expected 1 | Out-Null
    Assert-ProbeValue -Name '溜めたダメージを1回で受ける（量）' -Actual ([double]$end.healthBefore - [double]$end.healthAfter) -Expected $expectedDamage -Tolerance 0.01 | Out-Null
    Assert-ProbeTrue -Name '敵の停止が解ける' -Condition (-not [bool]$end.enemyPause) | Out-Null
    Assert-ProbeTrue -Name 'レイの停止が解ける' -Condition (-not [bool]$end.tracerFreezing) | Out-Null
    Assert-ProbeValue -Name '敵弾のトレイル寿命が元に戻る' -Actual ([double]$end.enemyTrailTime) -Expected ([double]$start.originalTrailTime) | Out-Null
    Assert-ProbeValue -Name '視点が本体へ戻る（X）' -Actual ([double]$end.localX) -Expected ([double]$start.defaultLocalX) -Tolerance 0.001 | Out-Null
    Assert-ProbeValue -Name '視点が本体へ戻る（Z）' -Actual ([double]$end.localZ) -Expected ([double]$start.defaultLocalZ) -Tolerance 0.001 | Out-Null

    Start-Sleep -Milliseconds 800

    $after = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface.DataStore;

var wave = LifetimeScope.Find<BattleLifetimeScope>().Container.Resolve<IWaveManagerDataStore>();
var enemyBullet = GameObject.Find("ProbeEnemyBullet");
return $"{{\"enemyBulletX\":{(enemyBullet != null ? enemyBullet.transform.position.x : 0f)},\"elapsed\":{wave.ElapsedTime.CurrentValue},\"isWavePause\":{wave.IsWavePause.CurrentValue.ToString().ToLower()}}}";
'@
    Assert-ProbeTrue -Name '終了後は敵弾が動き出す' -Condition ([double]$after.enemyBulletX -gt ([double]$end.enemyBulletX + 1)) `
        -Detail "($($end.enemyBulletX) → $($after.enemyBulletX))" | Out-Null
    Assert-ProbeTrue -Name '終了後はウェーブ経過時間が進む' -Condition ([bool]$after.isWavePause -or ([double]$after.elapsed -gt [double]$during.elapsed)) | Out-Null

    # --- 4. ウェーブ間ポーズで打ち切られる ---
    $wavePause = Invoke-UnityJson -Snippet @'
using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Battle.Views;

var c = LifetimeScope.Find<BattleLifetimeScope>().Container;
var overclock = c.Resolve<IOverclockDataStore>();
var player = c.Resolve<IPlayerStateDataStore>();
var wave = c.Resolve<IWaveManagerDataStore>();
var buff = c.Resolve<IBuffStateDataStore>();
var playerView = c.Resolve<IBattlePlayerView>() as MonoBehaviour;
var receiver = playerView.GetComponentInChildren<PlayerDamageReceiverView>();

overclock.AddStock(12f);
receiver.OnHit(5f, 999, player.Position.Value, out _);
var healthBefore = player.Health.Value;
var multiply = buff.CalcDamageTakenMultiply();

wave.SetWavePause(true);
var active = overclock.IsActive.CurrentValue;
var healthAfter = player.Health.Value;
wave.SetWavePause(false);

// 片付け
foreach (var name in new[] { "ProbeEnemyBullet", "ProbePlayerBullet" })
{
    var go = GameObject.Find(name);
    if (go != null) UnityEngine.Object.Destroy(go);
}

return $"{{\"active\":{active.ToString().ToLower()},\"healthBefore\":{healthBefore},\"healthAfter\":{healthAfter},\"multiply\":{multiply}}}";
'@
    Assert-ProbeTrue -Name 'ウェーブ間ポーズで終わる' -Condition (-not [bool]$wavePause.active) | Out-Null
    Assert-ProbeValue -Name 'ウェーブ間ポーズでも溜めたダメージを受ける' -Actual ([double]$wavePause.healthBefore - [double]$wavePause.healthAfter) `
        -Expected ([math]::Max(1, 5 * [double]$wavePause.multiply)) -Tolerance 0.01 | Out-Null
}
