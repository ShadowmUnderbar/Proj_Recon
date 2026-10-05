using App.Common.Interface;
using App.Common.Presenters;
using App.Common.UseCase;
using App.Common.Views;
using App.MainMenu.Interface;
using App.MainMenu.Presenters;
using App.MainMenu.UseCase;
using App.MainMenu.Views;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu
{
    /// <summary>
    /// メインメニューシーンのスコープ。
    /// セーブデータ・マスターデータ・シーン遷移は常駐スコープ（CommonLifetimeScope）から解決する。
    ///
    /// UIパネルとXRリグは部屋の決まった位置に置くものなので、プレハブ生成ではなく
    /// シーンに配置したものをそのまま解決する。
    /// </summary>
    public class MainMenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            #region UseCase

            builder.RegisterEntryPoint<MainMenuUseCase>();
            builder.RegisterEntryPoint<MenuLocomotionUseCase>();
            builder.RegisterEntryPoint<OptionUseCase>();
            // チュートリアルメッセージ（バトルと共通）。セット選択の説明を MainMenuUseCase から出す
            builder.RegisterEntryPoint<TutorialMessageUseCase>().As<ITutorialMessageUseCase>();

            #endregion

            #region Presenter

            builder.Register<MainMenuPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMainMenuPresenter>();
            // START後のアップグレードセット選択。バトル側と同じView/Presenterを使う
            builder.Register<RunStartPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IRunStartPresenter>();
            builder.Register<MenuLocomotionPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMenuLocomotionPresenter>();
            builder.Register<OptionPanelPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IOptionPanelPresenter>();
            builder.Register<TutorialMessagePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<ITutorialMessagePresenter>();
            // チュートリアルメッセージの追従先（リグの頭と非利き手）
            builder.Register<MenuPlayerPosePresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IPlayerPosePresenter>();

            #endregion

            #region View

            builder.RegisterComponentInHierarchy<MainMenuView>()
                .AsImplementedInterfaces().As<IMainMenuView>();
            // STARTパネル（MainMenuView.prefab）内のセット選択部分
            builder.RegisterComponentInHierarchy<RunStartView>()
                .AsImplementedInterfaces().As<IRunStartView>();
            builder.RegisterComponentInHierarchy<MenuLocomotionView>()
                .AsImplementedInterfaces().As<IMenuLocomotionView>();
            builder.RegisterComponentInHierarchy<OptionPanelView>()
                .AsImplementedInterfaces().As<IOptionPanelView>();
            builder.RegisterComponentInHierarchy<MenuPlayerPoseView>()
                .AsImplementedInterfaces().As<IMenuPlayerPoseView>();
            // チュートリアルメッセージ（WorldSpace Canvas）。バトルと同じプレハブをシーンに置く
            builder.RegisterComponentInHierarchy<TutorialMessageView>()
                .AsImplementedInterfaces().As<ITutorialMessageView>();

            #endregion
        }
    }
}
