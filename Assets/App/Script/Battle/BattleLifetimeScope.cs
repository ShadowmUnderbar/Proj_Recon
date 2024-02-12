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

namespace App.Battle
{
    public class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private PlayerMoveView _playerMoveView;
        [SerializeField]
        private PlayerTopDownAimStoreView _tpoDownAimStoreView;
        [SerializeField]
        private TestBulletView _testBulletView;
        [SerializeField]
        private PlayerShotView _playerShot;
        [SerializeField]
        private PlayerAimMuzzleView _playerAimMuzzleView;
        [SerializeField]
        private PlayerTopDownAimView _playerTopDownAimView;

        protected override void Configure(IContainerBuilder builder)
        {
            /*
            builder.RegisterComponentOnNewGameObject<TStore>(Lifetime.Singleton)
                .UnderTransform(transform)
                .AsImplementedInterfaces()
                .As<TInterface>();
             */

            builder.RegisterEntryPoint<PlayerMoveUseCase>().As<IPlayerMoveUseCase>();
            builder.RegisterEntryPoint<PlayerAimUseCase>().As<IPlayerAimUseCase>();
            builder.RegisterEntryPoint<PlayerShotUseCase>().As<IPlayerShotUseCase>();

            builder.Register<PlayerControlPresenter>(Lifetime.Singleton).AsImplementedInterfaces().As<IPlayerControlPresenter>();

            builder.RegisterComponent(_playerMoveView).AsImplementedInterfaces().As<IPlayerMoveView>();

            builder.RegisterComponentInNewPrefab(_tpoDownAimStoreView, Lifetime.Singleton).UnderTransform(transform).AsImplementedInterfaces().As<IPlayerTopDownAimStoreView>();

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
        }
    }
}