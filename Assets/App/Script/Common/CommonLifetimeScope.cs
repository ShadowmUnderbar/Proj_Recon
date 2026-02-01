using App.Common.Interface;
using App.Common.Data.Database;
using App.Common.DataStore;
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

            builder.Register<PlayerSettingDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerSettingDataStore>();
            builder.Register<SaveDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ISaveDataStore>();
            builder.Register<CoreSkillUnlockDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ICoreSkillUnlockDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<GameInputDataStore>().As<IGameInputDataStore>();

            #endregion

            #region Database

            builder.RegisterInstance(_enemyDatabase);
            builder.RegisterInstance(_enemySpawnDatabase);

            #endregion

#if UNITY_EDITOR
            if (!EditorPrefs.GetBool("VRMode", false))
            {
                return;
            }
#endif

            InitXR().Forget();
        }

        private static async UniTask InitXR()
        {
            await XRGeneralSettings.Instance.Manager.InitializeLoader();
            while (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                await UniTask.Yield();
            }

            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }
}