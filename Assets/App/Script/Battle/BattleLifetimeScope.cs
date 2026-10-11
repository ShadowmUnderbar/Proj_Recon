using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Presenters;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Data;
using App.Common.Interface;
using App.Common.Presenters;
using App.Common.UseCase;
using App.Common.Views;
using App.Framework.Utilities;
using App.Battle.DataStore;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle
{
    public class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField] private BattlePlayerView _playerView;
        [SerializeField] private PlayerShotView _playerShot;
        [SerializeField] private PlayerBulletView _playerBulletView;
        [SerializeField] private PlayerAimMuzzleView _playerAimMuzzleView;
        [SerializeField] private PlayerTopDownAimView _playerTopDownAimView;
        [SerializeField] private EnemyStoreView _enemyStoreView;
        [SerializeField] private HitBoxStoreView _hitBoxStoreView;
        [SerializeField] private BlitzEffectView _blitzEffectView;
        [SerializeField] private BulletTracerView _bulletTracerView;
        [SerializeField] private CounterTracerView _counterTracerView;
        [SerializeField] private BulletStoreView _bulletStoreView;
        [SerializeField] private ShopView _shopView;
        [SerializeField] private GameOverView _gameOverView;
        [SerializeField] private PlayerLifeGaugeView _playerLifeGaugeView;
        [SerializeField] private RunStartView _runStartView;
        [SerializeField] private TutorialMessageView _tutorialMessageView;
        [SerializeField] private WaveConfig _waveConfig;
        [SerializeField] private StreamerCameraView _streamerCameraView;
        [SerializeField] private StreamerCameraTriggerConfig _streamerCameraTriggerConfig;
        [SerializeField] private DodgeCounterAttackConfig _dodgeCounterAttackConfig;
        [SerializeField] private PointParticleStoreView _pointParticleStoreView;
        [SerializeField] private PointParticleConfig _pointParticleConfig;
        [SerializeField] private PointDropConfig _pointDropConfig;
        [SerializeField] private PlayerDeathConfig _playerDeathConfig;
        [SerializeField] private GameClearConfig _gameClearConfig;
        [SerializeField] private PlayerBaseParameterConfig _playerBaseParameterConfig;
        [SerializeField] private TutorialWaveConfig _tutorialWaveConfig;
        [SerializeField] private OverclockConfig _overclockConfig;
        [SerializeField] private SepiaToneConfig _sepiaToneConfig;
        [SerializeField] private TimeStopConfig _timeStopConfig;
        [SerializeField] private BossWaveConfig _bossWaveConfig;
        [SerializeField] private BossLifeGaugeStoreView _bossLifeGaugeStoreView;
        [SerializeField] private BossLifeGaugeConfig _bossLifeGaugeConfig;
        [SerializeField] private EnemySpawnConfig _enemySpawnConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            // 登録順序がTick順序に影響するため、依存順に登録
            builder.Register<FreezeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IFreezeDataStore>();
            // オーバークロック（敵・敵弾の停止）。バフ・スポーン等の時間進行がこの発動状態を見るため先に登録する
            builder.Register<OverclockDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IOverclockDataStore>();

            // セピア調の演出（オーバークロックなどがプリセットを指定してかける）
            builder.Register<SepiaToneDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ISepiaToneDataStore>();

            // ボスによる時止め（プレイヤーと弾だけを止める。ボスの台本が始める・解く）
            builder.Register<TimeStopDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ITimeStopDataStore>();
            builder.Register<PlayerStateDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerStateDataStore>();
            builder.Register<PlayerFocusDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerFocusDataStore>();
            builder.Register<PlayerAimDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerAimDataStore>();
            builder.Register<PlayerShotTypeDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerShotTypeDataStore>();
            // 敵のスポーン時HP・攻撃力の決定に使う（EnemyDataStoreが依存）
            builder.Register<EnemyWaveScalingCalculatorDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemyWaveScalingCalculatorDataStore>();
            builder.Register<EnemyDataStore>(Lifetime.Singleton).AsImplementedInterfaces().As<IEnemyDataStore>();
            builder.Register<EnemyRandomSpawnCycleDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemyRandomSpawnCycleDataStore>();
            // 複数個体のボスの台本進行（EnemyDataStoreへ個体を登録する）
            builder.Register<BossGroupDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBossGroupDataStore>();
            // ボスウェーブの判定（湧き周期・ウェーブ進行が参照する）
            builder.Register<BossWaveDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBossWaveDataStore>();
            builder.Register<PeaceMakerDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPeaceMakerDataStore>();
            builder.Register<AvalancheDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IAvalancheDataStore>();
            builder.Register<PlayerBulletParameterDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerBulletParameterDataStore>();
            builder.Register<PlayerDodgeParameterDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerDodgeParameterDataStore>();
            builder.Register<DodgeCounterAttackDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IDodgeCounterAttackDataStore>();
            builder.Register<ElectricShockDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IElectricShockDataStore>();
            builder.Register<ShotConflictDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IShotConflictDataStore>();
            builder.Register<UpgradeSessionDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IUpgradeSessionDataStore>();
            builder.Register<BuffStateDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBuffStateDataStore>();
            builder.Register<UpgradeEffectSimpleCalculatorDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IUpgradeEffectSimpleCalculatorDataStore>();
            builder.Register<HealOnKillDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IHealOnKillDataStore>();
            builder.Register<CriticalHitDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ICriticalHitDataStore>();
            builder.Register<SnakeEyesDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ISnakeEyesDataStore>();
            builder.Register<MedusaDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMedusaDataStore>();
            builder.Register<MeanMugDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMeanMugDataStore>();
            builder.Register<BullseyeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBullseyeDataStore>();
            builder.Register<DependencyNodeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IDependencyNodeDataStore>();
            builder.Register<DamageNodeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IDamageNodeDataStore>();
            builder.Register<CareNodeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ICareNodeDataStore>();
            builder.Register<EmergencyNodeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEmergencyNodeDataStore>();
            builder.Register<PlayerBarrierDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerBarrierDataStore>();
            builder.Register<UpgradeLotteryDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IUpgradeLotteryDataStore>();
            // アップグレードのローカライズ文言（Localization の Upgrade テーブル）
            builder.Register<UpgradeLocalizationDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IUpgradeLocalizationDataStore>();
            builder.Register<WaveManagerDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IWaveManagerDataStore>();
            builder.Register<GameStateDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IGameStateDataStore>();
            builder.Register<RunStartDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunStartDataStore>();
            builder.Register<PointDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPointDataStore>();
            builder.Register<PointDropCalculatorDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPointDropCalculatorDataStore>();
            builder.Register<StreamerCameraDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IStreamerCameraDataStore>();
            // デバッグ対戦で出した相手と出し直しの待ち（通常のランでは使われない）
            builder.Register<DebugArenaDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IDebugArenaDataStore>();

            #endregion

            #region Shared

            // アップグレード付与副作用の共通処理（ShopUseCase・RunStartUseCaseが利用）
            builder.Register<UpgradeSideEffectApplier>(Lifetime.Singleton);

            // 結果画面の獲得アップグレード一覧の組み立て（RunResultUseCaseが利用）
            builder.Register<AcquiredUpgradeListBuilder>(Lifetime.Singleton).As<IAcquiredUpgradeListBuilder>();

            // ラン状態の一括リセット（RunResultUseCaseのリスタートが利用）
            builder.Register<RunResetUseCase>(Lifetime.Singleton);

            // 配信用カメラのフレーミング計算（StreamerCameraViewが利用）
            builder.Register<StreamerCameraFramingCalculator>(Lifetime.Singleton);

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<PlayerMoveUseCase>();
            builder.RegisterEntryPoint<PlayerAimUseCase>();
            builder.RegisterEntryPoint<PlayerShotUseCase>();
            builder.RegisterEntryPoint<EnemySpawnUseCase>();
            builder.RegisterEntryPoint<EnemyControlUseCase>();
            builder.RegisterEntryPoint<PlayerGazeUseCase>();
            // BattleHitUseCase は撃破通知の中で敵データを消すため、
            // 撃破地点を参照するポイントドロップを先に購読させる（購読順＝登録順）
            builder.RegisterEntryPoint<PointDropUseCase>();
            builder.RegisterEntryPoint<BattleHitUseCase>();
            builder.RegisterEntryPoint<PlayerHitUseCase>();
            builder.RegisterEntryPoint<PlayerDodgeUseCase>();
            builder.RegisterEntryPoint<FreezeUseCase>();
            builder.RegisterEntryPoint<OverclockUseCase>();
            // セピア調の強さをシェーダへ配る
            builder.RegisterEntryPoint<SepiaToneUseCase>();
            builder.RegisterEntryPoint<TimeStopEffectUseCase>();
            // ボスグループの台本進行と個体をつなぐ（フリーズ・ウェーブ間ポーズ中は台本を止める）
            builder.RegisterEntryPoint<BossGroupUseCase>();
            // ボスウェーブ開始時に残った敵を消し、プレイヤーを移してボスを出す
            builder.RegisterEntryPoint<BossWaveUseCase>();
            // デバッグ対戦（エディタの「App/デバッグ: 敵と対戦」）で指定の相手を出し、倒したら出し直す
            builder.RegisterEntryPoint<DebugArenaUseCase>();
            // ボスの足元の体力ゲージ（プレイヤーのライフゲージを流用）
            builder.RegisterEntryPoint<BossLifeGaugeUseCase>();
            builder.RegisterEntryPoint<DodgeCounterAttackUseCase>();
            builder.RegisterEntryPoint<EnemyRandomSpawnUseCase>();
            builder.RegisterEntryPoint<WaveManagerUseCase>();
            builder.RegisterEntryPoint<ShopUseCase>();
            builder.RegisterEntryPoint<BuffConditionUseCase>();
            builder.RegisterEntryPoint<CareNodeUseCase>();
            // ランの結果画面（スロット保存・リスタート・メインメニュー）。ゲームオーバーとクリアで共用する
            builder.RegisterEntryPoint<RunResultUseCase>().AsSelf();
            builder.RegisterEntryPoint<GameOverUseCase>();
            // ボスを倒したらクリア表示 → 結果画面
            builder.RegisterEntryPoint<GameClearUseCase>();
            builder.RegisterEntryPoint<PlayerLifeGaugeUseCase>();
            builder.RegisterEntryPoint<RunStartUseCase>();
            builder.RegisterEntryPoint<StreamerCameraUseCase>();
            // チュートリアルメッセージの表示手段。表示のきっかけを持つ側（ウェーブ開始・ショップ・結果画面・セット選択）が
            // ITutorialMessageUseCase を注入して ShowIfNeeded/Hide を呼ぶ。頭と手の姿勢は PlayerControlPresenter（IPlayerPosePresenter）から取る
            builder.RegisterEntryPoint<TutorialMessageUseCase>().As<ITutorialMessageUseCase>();
            // ウェーブ開始時に TutorialWaveConfig の割り当てに従ってチュートリアルを出す
            builder.RegisterEntryPoint<TutorialWaveUseCase>();

            #endregion

            #region Presenter

            builder.Register<PlayerControlPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerControlPresenter>();

            builder.Register<EnemyPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemyPresenter>();
            builder.Register<BattleHitPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBattleHitPresenter>();
            builder.Register<ShopPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IShopPresenter>();
            builder.Register<GameOverPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IGameOverPresenter>();
            builder.Register<PlayerLifeGaugePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerLifeGaugePresenter>();
            builder.Register<BossLifeGaugePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBossLifeGaugePresenter>();
            builder.Register<RunStartPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunStartPresenter>();
            builder.Register<StreamerCameraPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IStreamerCameraPresenter>();
            builder.Register<PointParticlePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPointParticlePresenter>();
            builder.Register<TutorialMessagePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ITutorialMessagePresenter>();

            #endregion

            #region SingletonView

            builder.RegisterComponentInNewPrefab(_playerView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBattlePlayerView>();

            builder.RegisterComponentInNewPrefab(_shopView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IShopView>();

            builder.RegisterComponentInNewPrefab(_gameOverView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IGameOverView>();

            // 足元の半円ライフゲージ。プレイヤー位置へはUseCase経由で追従させる
            builder.RegisterComponentInNewPrefab(_playerLifeGaugeView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IPlayerLifeGaugeView>();

            // ボスごとの足元の体力ゲージ。生成・追従はUseCase経由
            builder.RegisterComponentInNewPrefab(_bossLifeGaugeStoreView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBossLifeGaugeStoreView>();

            builder.RegisterComponentInNewPrefab(_runStartView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IRunStartView>();

            // チュートリアルメッセージ（WorldSpace Canvas）。追従先の姿勢は UseCase から毎フレーム渡す
            builder.RegisterComponentInNewPrefab(_tutorialMessageView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<ITutorialMessageView>();

            // 配信用カメラ。ストリーマーモード無効時はView側で自身を無効化する
            builder.RegisterComponentInNewPrefab(_streamerCameraView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IStreamerCameraView>();

            #endregion

            #region View

            builder.Register<SimpleObjectFactory<IBulletView, PlayerBulletView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<IBulletView>>()
                .WithParameter("prefab", _playerBulletView);

            builder.Register<SimpleObjectFactory<IPlayerShotView, PlayerShotView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<IPlayerShotView>>()
                .WithParameter("prefab", _playerShot);

            builder.Register<SimpleObjectFactory<IPlayerAimMuzzleView, PlayerAimMuzzleView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<IPlayerAimMuzzleView>>()
                .WithParameter("prefab", _playerAimMuzzleView);

            builder.Register<SimpleObjectFactory<IPlayerTopDownAimView, PlayerTopDownAimView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<IPlayerTopDownAimView>>()
                .WithParameter("prefab", _playerTopDownAimView);

            // 注視判定を LateUpdate で回すため、new ではなくプレハブから GameObject として生成する
            builder.RegisterComponentInNewPrefab(_enemyStoreView, Lifetime.Singleton).UnderTransform(transform)
                .As<IEnemyStoreView>();

            builder.Register<HitBoxStoreView>(Lifetime.Singleton)
                .As<IHitBoxStoreView>()
                .WithParameter("prefab", _hitBoxStoreView);

            builder.Register<SimpleObjectFactory<BlitzEffectView, BlitzEffectView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<BlitzEffectView>>()
                .WithParameter("prefab", _blitzEffectView);

            builder.Register<SimpleObjectFactory<BulletTracerView, BulletTracerView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<BulletTracerView>>()
                .WithParameter("prefab", _bulletTracerView);

            builder.Register<TracerFreezeState>(Lifetime.Singleton).As<ITracerFreezeState>();

            // セピア調のグローバル変数を配る（破棄時に効果なしへ戻す）
            builder.Register<SepiaToneView>(Lifetime.Singleton).As<ISepiaToneView>();

            builder.Register<SimpleObjectFactory<CounterTracerView, CounterTracerView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<CounterTracerView>>()
                .WithParameter("prefab", _counterTracerView);

            builder.RegisterComponentInNewPrefab(_bulletStoreView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBulletStoreView>();

            builder.RegisterComponentInNewPrefab(_pointParticleStoreView, Lifetime.Singleton)
                .UnderTransform(transform)
                .AsImplementedInterfaces().As<IPointParticleStoreView>();

            builder.RegisterInstance(_waveConfig);

            // デバッグ設定は組み立て時にだけ DebugConfig から読み、利用側へは注入で渡す
            builder.RegisterInstance(new EnemyGazeDebugSettings(DebugConfig.IsGazeTouchHitFeedback));
            // デバッグ対戦の予約は組み立て時に1回だけ取り出す（リスタートはこの設定のまま続く）
            builder.RegisterInstance(CreateDebugArenaSettings());
            builder.RegisterInstance(_streamerCameraTriggerConfig);
            builder.RegisterInstance(_dodgeCounterAttackConfig);
            builder.RegisterInstance(_pointParticleConfig);
            builder.RegisterInstance(_pointDropConfig);
            builder.RegisterInstance(_playerDeathConfig);
            // クリア表示の見出しと、結果画面のボタンを出すまでの秒数（GameClearUseCase が利用）
            builder.RegisterInstance(_gameClearConfig);
            // プレイヤー基礎パラメータ（体力・射撃倍率・回避）。PlayerState/PlayerBulletParameter/PlayerDodgeParameter の各DataStoreが利用
            builder.RegisterInstance(_playerBaseParameterConfig);
            // ウェーブ開始時のチュートリアル割り当て（TutorialWaveUseCase が利用）
            builder.RegisterInstance(_tutorialWaveConfig);
            // オーバークロックの発動しきい値（OverclockDataStore が利用）
            builder.RegisterInstance(_overclockConfig);
            // 時止めの演出（TimeStopEffectUseCase が利用）
            builder.RegisterInstance(_timeStopConfig);
            // セピア調の色味（SepiaToneView が利用）
            builder.RegisterInstance(_sepiaToneConfig);
            // ボスウェーブの番号・ボスグループ・出現位置（BossWaveDataStore / BossWaveUseCase が利用）
            builder.RegisterInstance(_bossWaveConfig);
            // ボスの体力ゲージの色・大きさ（BossLifeGaugeStoreView が利用）
            builder.RegisterInstance(_bossLifeGaugeConfig);
            // 雑魚の最低生存数（EnemyRandomSpawnCycleDataStore が利用）
            builder.RegisterInstance(_enemySpawnConfig);

            #endregion
        }

        /// <summary>
        /// DebugConfig のデバッグ対戦の予約から設定を作る。予約が無い・読めないときは通常のラン（Disabled）。
        /// ボスグループはアセットパスで受け取るため、エディタでしか解決できない（製品ビルドでは予約が常に無い）
        /// </summary>
        private static DebugArenaSettings CreateDebugArenaSettings()
        {
            var json = DebugConfig.ConsumeDebugArenaRequestJson();
            if (string.IsNullOrEmpty(json))
            {
                return DebugArenaSettings.Disabled;
            }

            DebugArenaRequest request;
            try
            {
                request = JsonUtility.FromJson<DebugArenaRequest>(json);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError($"[BattleLifetimeScope] デバッグ対戦の予約を読めないため通常のランで開始します: {e.Message}");
                return DebugArenaSettings.Disabled;
            }

            BossGroupConfig bossGroup = null;
            if (request.IsBossGroup)
            {
#if UNITY_EDITOR
                bossGroup = UnityEditor.AssetDatabase.LoadAssetAtPath<BossGroupConfig>(request.BossGroupAssetPath);
#endif
                if (bossGroup == null)
                {
                    Debug.LogError($"[BattleLifetimeScope] デバッグ対戦のボスグループが見つからないため通常のランで開始します: {request.BossGroupAssetPath}");
                    return DebugArenaSettings.Disabled;
                }
            }

            return new DebugArenaSettings(
                true,
                bossGroup,
                request.EnemyCode,
                request.EnemyCount,
                request.Wave,
                request.AutoRespawn,
                request.RespawnDelaySeconds,
                request.Invincible);
        }
    }
}
