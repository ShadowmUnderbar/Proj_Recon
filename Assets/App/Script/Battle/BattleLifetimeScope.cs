using App.Battle.Interface;
using App.Battle.Presenters;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Common.Data;
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
        [SerializeField] private BulletStoreView _bulletStoreView;
        [SerializeField] private ShopView _shopView;
        [SerializeField] private GameOverView _gameOverView;
        [SerializeField] private RunStartView _runStartView;
        [SerializeField] private WaveConfig _waveConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            // 登録順序がTick順序に影響するため、依存順に登録
            builder.Register<PlayerStateDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerStateDataStore>();
            builder.Register<PlayerFocusDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerFocusDataStore>();
            builder.Register<PlayerAimDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerAimDataStore>();
            builder.Register<PlayerShotTypeDataStore>(Lifetime.Singleton)
                .AsImplementedInterfaces().As<IPlayerShotTypeDataStore>();
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
            builder.Register<WaveManagerDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IWaveManagerDataStore>();
            builder.Register<GameStateDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IGameStateDataStore>();
            builder.Register<RunStartDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunStartDataStore>();

            #endregion

            #region Shared

            // アップグレード付与副作用の共通処理（ShopUseCase・RunStartUseCaseが利用）
            builder.Register<UpgradeSideEffectApplier>(Lifetime.Singleton);

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<PlayerMoveUseCase>();
            builder.RegisterEntryPoint<PlayerAimUseCase>();
            builder.RegisterEntryPoint<PlayerShotUseCase>();
            builder.RegisterEntryPoint<EnemySpawnUseCase>();
            builder.RegisterEntryPoint<EnemyControlUseCase>();
            builder.RegisterEntryPoint<PlayerGazeUseCase>();
            builder.RegisterEntryPoint<BattleHitUseCase>();
            builder.RegisterEntryPoint<PlayerHitUseCase>();
            builder.RegisterEntryPoint<PlayerDodgeUseCase>();
            builder.RegisterEntryPoint<EnemyRandomSpawnUseCase>();
            builder.RegisterEntryPoint<WaveManagerUseCase>();
            builder.RegisterEntryPoint<ShopUseCase>();
            builder.RegisterEntryPoint<BuffConditionUseCase>();
            builder.RegisterEntryPoint<CareNodeUseCase>();
            builder.RegisterEntryPoint<GameOverUseCase>();
            builder.RegisterEntryPoint<RunStartUseCase>();

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
            builder.Register<RunStartPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunStartPresenter>();

            #endregion

            #region SingletonView

            builder.RegisterComponentInNewPrefab(_playerView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBattlePlayerView>();

            builder.RegisterComponentInNewPrefab(_shopView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IShopView>();

            builder.RegisterComponentInNewPrefab(_gameOverView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IGameOverView>();

            builder.RegisterComponentInNewPrefab(_runStartView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IRunStartView>();

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

            builder.RegisterComponentInNewPrefab(_bulletStoreView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBulletStoreView>();

            builder.RegisterInstance(_waveConfig);

            #endregion
        }
    }
}