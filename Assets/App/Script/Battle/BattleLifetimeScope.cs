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
        private PlayerMove _playerMove;
        [SerializeField]
        private PlayerTopDownAimStoreView _tpoDownAimStoreView;
        [SerializeField]
        private TestBullet _testBullet;

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

            builder.RegisterComponent(_playerMove).AsImplementedInterfaces().As<IPlayerMove>();

            builder.RegisterComponent(_tpoDownAimStoreView).AsImplementedInterfaces().As<IPlayerTopDownAimStoreView>();

            builder.Register<SimpleObjectFactory<ITestBullet, TestBullet>>(Lifetime.Singleton)
                .As<ISimpleObjectFactory<ITestBullet>>()
                .WithParameter("prefab", _testBullet);
        }
    }
}