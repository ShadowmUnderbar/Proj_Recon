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
        [SerializeField] private BulletDataBase _bulletDataBase;
        [SerializeField] private EnemyDatabase _enemyDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            builder.Register<PlayerSettingDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerSettingDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<GameInputUsecase>().As<IGameInputUsecase>();
#if OCULUS
            builder.RegisterEntryPoint<OVRInputUseCase>().As<IOVRInputUseCase>();
#endif

            #endregion

            #region Database

            builder.RegisterInstance(_bulletDataBase);
            builder.RegisterInstance(_enemyDatabase);

            #endregion
        }
    }
}