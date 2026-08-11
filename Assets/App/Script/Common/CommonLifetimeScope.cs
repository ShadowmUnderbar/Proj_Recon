using App.Common.Data;
using App.Common.Interface;
using App.Common.Data.Database;
using App.Common.DataStore;
using App.Common.UseCase;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Common
{
    public class CommonLifetimeScope : LifetimeScope
    {
        [SerializeField] private EnemyDatabase _enemyDatabase;
        [SerializeField] private EnemySpawnDatabase _enemySpawnDatabase;
        [SerializeField] private UpgradeDatabase _upgradeDatabase;
        [SerializeField] private BuffDatabase _buffDatabase;
        [SerializeField] private StreamerModeConfig _streamerModeConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            #region DataStore

            builder.Register<PlayerSettingDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerSettingDataStore>();
            builder.Register<SaveDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ISaveDataStore>();
            builder.Register<CoreSkillUnlockDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ICoreSkillUnlockDataStore>();
            builder.Register<MetaProgressionDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMetaProgressionDataStore>();
            builder.RegisterEntryPoint<GameInputDataStore>()
                .As<IGameInputDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<XRInitUseCase>();

            // ストリーマーモードのディスプレイ出力切り替え（ミラー表示の抑制）
            builder.RegisterEntryPoint<StreamerDisplayUseCase>();

            #endregion

            #region Config

            // 配信用カメラの出力設定。BattleLifetimeScope（子スコープ）からも解決される
            builder.RegisterInstance(_streamerModeConfig);

            #endregion

            #region Database

            builder.RegisterInstance(_enemyDatabase);
            builder.RegisterInstance(_enemySpawnDatabase);
            builder.RegisterInstance(_upgradeDatabase);
            builder.RegisterInstance(_buffDatabase);

            #endregion
        }
    }
}