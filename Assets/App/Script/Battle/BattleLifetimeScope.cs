using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Presenters;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Data;
using App.Common.Interface;
using App.Common.Presenters;
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
        [SerializeField] private WaveConfig _waveConfig;
        [SerializeField] private StreamerCameraView _streamerCameraView;
        [SerializeField] private StreamerCameraTriggerConfig _streamerCameraTriggerConfig;
        [SerializeField] private DodgeCounterAttackConfig _dodgeCounterAttackConfig;
        [SerializeField] private PointParticleStoreView _pointParticleStoreView;
        [SerializeField] private PointParticleConfig _pointParticleConfig;
        [SerializeField] private PointDropConfig _pointDropConfig;
        [SerializeField] private PlayerDeathConfig _playerDeathConfig;
        [SerializeField] private UpgradeDescriptionStyle _upgradeDescriptionStyle;
        [SerializeField] private PlayerBaseParameterConfig _playerBaseParameterConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            // 登録順序がTick順序に影響するため、依存順に登録
            builder.Register<FreezeDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IFreezeDataStore>();
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

            #endregion

            #region Shared

            // アップグレード付与副作用の共通処理（ShopUseCase・RunStartUseCaseが利用）
            builder.Register<UpgradeSideEffectApplier>(Lifetime.Singleton);

            // ラン状態の一括リセット（GameOverUseCaseのリスタートが利用）
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
            builder.RegisterEntryPoint<DodgeCounterAttackUseCase>();
            builder.RegisterEntryPoint<EnemyRandomSpawnUseCase>();
            builder.RegisterEntryPoint<WaveManagerUseCase>();
            builder.RegisterEntryPoint<ShopUseCase>();
            builder.RegisterEntryPoint<BuffConditionUseCase>();
            builder.RegisterEntryPoint<CareNodeUseCase>();
            builder.RegisterEntryPoint<GameOverUseCase>();
            builder.RegisterEntryPoint<PlayerLifeGaugeUseCase>();
            builder.RegisterEntryPoint<RunStartUseCase>();
            builder.RegisterEntryPoint<StreamerCameraUseCase>();

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
            builder.Register<RunStartPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunStartPresenter>();
            builder.Register<StreamerCameraPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IStreamerCameraPresenter>();
            builder.Register<PointParticlePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPointParticlePresenter>();

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

            builder.RegisterComponentInNewPrefab(_runStartView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IRunStartView>();

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

            builder.Register<EnemyStoreView>(Lifetime.Singleton)
                .As<IEnemyStoreView>()
                .WithParameter("prefab", _enemyStoreView);

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

            builder.Register<SimpleObjectFactory<CounterTracerView, CounterTracerView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<CounterTracerView>>()
                .WithParameter("prefab", _counterTracerView);

            builder.RegisterComponentInNewPrefab(_bulletStoreView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBulletStoreView>();

            builder.RegisterComponentInNewPrefab(_pointParticleStoreView, Lifetime.Singleton)
                .UnderTransform(transform)
                .AsImplementedInterfaces().As<IPointParticleStoreView>();

            builder.RegisterInstance(_waveConfig);
            builder.RegisterInstance(_streamerCameraTriggerConfig);
            builder.RegisterInstance(_dodgeCounterAttackConfig);
            builder.RegisterInstance(_pointParticleConfig);
            builder.RegisterInstance(_pointDropConfig);
            builder.RegisterInstance(_playerDeathConfig);
            // プレイヤー基礎パラメータ（体力・射撃倍率・回避）。PlayerState/PlayerBulletParameter/PlayerDodgeParameter の各DataStoreが利用
            builder.RegisterInstance(_playerBaseParameterConfig);
            // アップグレード詳細説明の効果値の装飾（UpgradeLocalizationDataStore が利用）
            builder.RegisterInstance(_upgradeDescriptionStyle);

            #endregion
        }
    }
}