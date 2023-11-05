using UnityEngine;
using VContainer;
using VContainer.Unity;
using App.Battle.UseCase;
using App.Battle.Interface.UseCase;
using App.Battle.Interface.Presenters;
using App.Battle.Presenters;

namespace App.Battle
{
    public class BattleLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private PlayerAim _playerAim;
        [SerializeField]
        private PlayerMove _playerMove;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleLifetimeScope>(Lifetime.Singleton);

            /*
            builder.RegisterComponentOnNewGameObject<TStore>(Lifetime.Singleton)
                .UnderTransform(transform)
                .AsImplementedInterfaces()
                .As<TInterface>();
             */

            builder.RegisterEntryPoint<PlayerControlUseCase>().As<IPlayerControlUseCase>();

            builder.Register<PlayerControlPresenter>(Lifetime.Singleton).AsImplementedInterfaces().As<IPlayerControlPresenter>();

            builder.RegisterComponent(_playerAim).AsImplementedInterfaces().As<IPlayerAim>();
            builder.RegisterComponent(_playerMove).AsImplementedInterfaces().As<IPlayerMove>();

        }
    }
}