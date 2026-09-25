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

            #endregion

            #region Presenter

            builder.Register<MainMenuPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMainMenuPresenter>();
            builder.Register<MenuLocomotionPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMenuLocomotionPresenter>();
            builder.Register<OptionPanelPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IOptionPanelPresenter>();

            #endregion

            #region View

            builder.RegisterComponentInHierarchy<MainMenuView>()
                .AsImplementedInterfaces().As<IMainMenuView>();
            builder.RegisterComponentInHierarchy<MenuLocomotionView>()
                .AsImplementedInterfaces().As<IMenuLocomotionView>();
            builder.RegisterComponentInHierarchy<OptionPanelView>()
                .AsImplementedInterfaces().As<IOptionPanelView>();

            #endregion
        }
    }
}
