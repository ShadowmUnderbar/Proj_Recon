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
        [SerializeField] private PlayerBulletView _bulletView;
        [SerializeField] private PlayerShotView _playerShot;
        [SerializeField] private PlayerAimMuzzleView _playerAimMuzzleView;
        [SerializeField] private PlayerTopDownAimView _playerTopDownAimView;
        [SerializeField] private EnemyStoreView _enemyStoreView;
        [SerializeField] private HitBoxStoreView _hitBoxStoreView;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            builder.Register<PlayerDataStore>(Lifetime.Singleton).AsImplementedInterfaces().As<IPlayerDataStore>();
            builder.Register<EnemyDataStore>(Lifetime.Singleton).AsImplementedInterfaces().As<IEnemyDataStore>();
            builder.Register<EnemyRandomSpawnCycleDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemyRandomSpawnCycleDataStore>();
            builder.Register<PlayerBulletParameterDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerBulletParameterDataStore>();
            builder.Register<PlayerDodgeParameterDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerDodgeParameterDataStore>();

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
                .WithParameter("prefab", _bulletView);

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

            #endregion
        }
    }
}