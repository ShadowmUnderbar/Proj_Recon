using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle.UseCase;
using App.Battle.Interface.UseCase;
using App.Battle.Interface.Presenters;
using App.Battle.Presenters;
using App.Battle.Views;
using App.Battle.Interface.Views;
using App.Framework.Utilities;
using App.Script.Battle.Interface.UseCase;
using App.Script.Battle.Interface.Views;
using App.Script.Battle.UseCase;
using App.Script.Battle.Views;

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
            builder.RegisterEntryPoint<PlayerMoveUseCase>().As<IPlayerMoveUseCase>();
            builder.RegisterEntryPoint<PlayerAimUseCase>().As<IPlayerAimUseCase>();
            builder.RegisterEntryPoint<PlayerShotUseCase>().As<IPlayerShotUseCase>();
            builder.RegisterEntryPoint<EnemySpawnUseCase>().As<IEnemySpawnUseCase>();

            builder.Register<PlayerControlPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerControlPresenter>();

            builder.RegisterComponentInNewPrefab(_playerView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IBattlePlayerView>();

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

            builder.Register<EnemySpawnPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IEnemySpawnPresenter>();

            builder.Register<SimpleObjectFactory<IEnemyStoreView, EnemyStoreView>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<IEnemyStoreView>>()
                .WithParameter("prefab", _enemyStoreView);
        }
    }
}