using App.MainMenu.Interface;
using App.MainMenu.Presenters;
using App.MainMenu.UseCase;
using App.MainMenu.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu
{
    /// <summary>
    /// メインメニューシーンのスコープ。
    /// セーブデータ・マスターデータ・シーン遷移は常駐スコープ（CommonLifetimeScope）から解決する。
    /// </summary>
    public class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenuView _mainMenuView;

        protected override void Configure(IContainerBuilder builder)
        {
            #region UseCase

            builder.RegisterEntryPoint<MainMenuUseCase>();

            #endregion

            #region Presenter

            builder.Register<MainMenuPresenter>(Lifetime.Singleton).AsImplementedInterfaces()
                .As<IMainMenuPresenter>();

            #endregion

            #region View

            builder.RegisterComponentInNewPrefab(_mainMenuView, Lifetime.Singleton).UnderTransform(transform)
                .AsImplementedInterfaces().As<IMainMenuView>();

            #endregion
        }
    }
}
