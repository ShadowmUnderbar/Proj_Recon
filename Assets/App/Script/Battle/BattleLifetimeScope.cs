using App.Battle.Interface;
using App.Battle.Presenters;
using App.Battle.UseCase;
using App.Battle.Views;
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
            builder.Register<PlayerBulletParameterDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerBulletParameterDataStore>();
            builder.Register<PlayerDodgeParameterDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerDodgeParameterDataStore>();
            builder.Register<UpgradeSessionDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IUpgradeSessionDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<PlayerMoveUseCase>();
            builder.RegisterEntryPoint<PlayerAimUseCase>();
            builder.RegisterEntryPoint<PlayerShotUseCase>();
            builder.RegisterEntryPoint<EnemySpawnUseCase>();
            builder.RegisterEntryPoint<EnemyControlUseCase>();
            builder.RegisterEntryPoint<BattleHitUseCase>();
            builder.RegisterEntryPoint<PlayerDodgeUseCase>();
            builder.RegisterEntryPoint<EnemyRandomSpawnUseCase>();

            #endregion

            #region Presenter

            builder.Register<PlayerControlPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerControlPresenter>();

            builder.Register<EnemyPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemyPresenter>();
            builder.Register<BattleHitPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBattleHitPresenter>();

            #endregion

            #region SingletonView

            builder.RegisterComponentInNewPrefab(_playerView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBattlePlayerView>();

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

            #endregion
        }
    }
}