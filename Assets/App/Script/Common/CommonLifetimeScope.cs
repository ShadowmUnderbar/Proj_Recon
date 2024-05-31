using App.Common.Interface.UseCase;
using App.Common.UseCase;
using App.Script.Common.Data.Database;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Script.Common
{
    public class CommonLifetimeScope : LifetimeScope
    {
        [SerializeField] private EnemyDatabase _enemyDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            #region UseCase

            builder.RegisterEntryPoint<GameInputUsecase>().As<IGameInputUsecase>();
#if OCULUS
            builder.RegisterEntryPoint<OVRInputUseCase>().As<IOVRInputUseCase>();
#endif

            #endregion

            #region Database

            builder.RegisterInstance(_enemyDatabase);

            #endregion
        }
    }
}