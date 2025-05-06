using App.Common.Interface;
using App.Common.UseCase;
using App.Common.Data.Database;
using App.Script.Common.DataStore;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Common
{
    public class CommonLifetimeScope : LifetimeScope
    {
        [SerializeField] private EnemyDatabase _enemyDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            builder.Register<PlayerSettingDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerSettingDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<GameInputUseCase>().As<IGameInputUseCase>();

            #endregion

            #region Database

            builder.RegisterInstance(_enemyDatabase);

            #endregion
        }
    }
}