using App.Common.Data;
using App.Common.Interface;
using App.Common.Data.Database;
using App.Common.DataStore;
using App.Common.UseCase;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;
using VContainer;
using VContainer.Unity;

namespace App.Common
{
    public class CommonLifetimeScope : LifetimeScope
    {
        [SerializeField] private EnemyDatabase _enemyDatabase;
        [SerializeField] private EnemySpawnDatabase _enemySpawnDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            builder.Register<PlatformConfigDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlatformConfigDataStore>();
            builder.Register<PlayerSettingDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerSettingDataStore>();
            builder.Register<SaveDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ISaveDataStore>();
            builder.Register<CoreSkillUnlockDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ICoreSkillUnlockDataStore>();
            builder.RegisterEntryPoint<GameInputDataStore>()
                .As<IGameInputDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<XRInitUseCase>();

            #endregion

            #region Database

            builder.RegisterInstance(_enemyDatabase);
            builder.RegisterInstance(_enemySpawnDatabase);

            #endregion
        }
    }
}