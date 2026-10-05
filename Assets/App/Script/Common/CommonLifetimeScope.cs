using App.Common.Data;
using App.Common.Interface;
using App.Common.Data.Database;
using App.Common.DataStore;
using App.Common.UseCase;
using App.Common.Views;
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
        [SerializeField] private WaveScalingDatabase _waveScalingDatabase;
        [SerializeField] private StreamerModeConfig _streamerModeConfig;
        [SerializeField] private EffectTextStyle _effectTextStyle;
        [SerializeField] private ShotLineColorConfig _shotLineColorConfig;

        [SerializeField, Tooltip("注視判定の Store（このプレハブ上のコンポーネント）")]
        private GazeTargetStoreView _gazeTargetStoreView;

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
            // メインメニューで選んだアップグレードセットをバトルシーンへ運ぶ（シーンをまたぐので常駐）
            builder.Register<RunLoadoutDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunLoadoutDataStore>();
            // チュートリアルの閲覧回数（セーブデータ）と再表示設定から表示要否を判定する
            builder.Register<TutorialProgressDataStore>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ITutorialProgressDataStore>();
            // チュートリアル文言（Localization の TutorialText テーブル）。メインメニュー・バトル双方で使うため常駐
            builder.RegisterEntryPoint<TutorialLocalizationDataStore>()
                .As<ITutorialLocalizationDataStore>();
            builder.RegisterEntryPoint<GameInputDataStore>()
                .As<IGameInputDataStore>();

            #endregion

            #region UseCase

            builder.RegisterEntryPoint<XRInitUseCase>();

            // ストリーマーモードのディスプレイ出力切り替え（ミラー表示の抑制）
            builder.RegisterEntryPoint<StreamerDisplayUseCase>();

            // シーン遷移。常駐スコープに置くことでメインメニュー・バトルの双方から使える
            builder.Register<SceneTransitionUseCase>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ISceneTransitionUseCase>();

            #endregion

            #region View
            // 注視判定。常駐させ、メインメニュー・バトル双方の GazeTargetView が子スコープから解決する
            builder.RegisterComponent(_gazeTargetStoreView).As<IGazeTargetStoreView>();
            #endregion

            #region Config

            // 配信用カメラの出力設定。BattleLifetimeScope（子スコープ）からも解決される
            builder.RegisterInstance(_streamerModeConfig);
            // 文言中の色タグの文字色（TutorialLocalizationDataStore・BattleLifetimeScope の UpgradeLocalizationDataStore が利用）
            builder.RegisterInstance(_effectTextStyle);
            // 照準ラインの色（PlayerShotUseCase が利用。EffectTextStyle の色タグもこの色に連動する）
            builder.RegisterInstance(_shotLineColorConfig);
            // 文字色はレイと同じライン色アセットを見ていないと連動しない。片方だけ差し替えた場合に気づけるようにする
            if (_effectTextStyle != null && _effectTextStyle.ShotLineColorConfig != _shotLineColorConfig)
            {
                Debug.LogWarning(
                    "[CommonLifetimeScope] EffectTextStyle の ShotLineColorConfig がこのスコープのものと異なります。文言の色タグがレイの色と連動しません",
                    this);
            }

            #endregion

            #region Database

            builder.RegisterInstance(_enemyDatabase);
            builder.RegisterInstance(_enemySpawnDatabase);
            builder.RegisterInstance(_upgradeDatabase);
            builder.RegisterInstance(_buffDatabase);
            // ウェーブ強化の増加率テーブル。BattleLifetimeScope（子スコープ）から解決される
            builder.RegisterInstance(_waveScalingDatabase);

            #endregion
        }
    }
}