using App.Battle.Interface;
using App.Battle.Presenters;
using App.Battle.UseCase;
using App.Battle.Views;
using App.Framework.Utilities;
using App.Battle.DataStore;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle
{
    public class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField] private BattlePlayerView _playerView;
        [SerializeField] private TestBulletView _testBulletView;
        [SerializeField] private PlayerShotView _playerShot;
        [SerializeField] private PlayerAimMuzzleView _playerAimMuzzleView;
        [SerializeField] private PlayerTopDownAimView _playerTopDownAimView;
        [SerializeField] private EnemyStoreView _enemyStoreView;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            builder.Register<PlayerDataStore>(Lifetime.Singleton).AsImplementedInterfaces().As<IPlayerDataStore>();
            builder.Register<EnemyDataStore>(Lifetime.Singleton).AsImplementedInterfaces().As<IEnemyDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<PlayerMoveUseCase>().As<IPlayerMoveUseCase>();
            builder.RegisterEntryPoint<PlayerAimUseCase>().As<IPlayerAimUseCase>();
            builder.RegisterEntryPoint<PlayerShotUseCase>().As<IPlayerShotUseCase>();
            builder.RegisterEntryPoint<EnemySpawnUseCase>().As<IEnemySpawnUseCase>();
            builder.RegisterEntryPoint<BattleHitUseCase>().As<IBattleHitUseCase>();

            #endregion

            #region Presenter

            builder.Register<PlayerControlPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerControlPresenter>();

            builder.Register<EnemySpawnPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemySpawnPresenter>();
            builder.Register<BattleHitPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IBattleHitPresenter>();

            #endregion

            #region SingletonView

            builder.RegisterComponentInNewPrefab(_playerView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBattlePlayerView>();

            #endregion

            #region View

            builder.Register<SimpleObjectFactory<IBulletView, TestBulletView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<IBulletView>>()
                .WithParameter("prefab", _testBulletView);

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

            #endregion
        }
    }
}