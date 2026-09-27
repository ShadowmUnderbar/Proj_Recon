using App.MainMenu.Interface;
using R3;
using VContainer;

namespace App.MainMenu.Presenters
{
    public class MainMenuPresenter : IMainMenuPresenter
    {
        private readonly IMainMenuView _mainMenuView;

        public Observable<Unit> OnStart => _mainMenuView.OnStart;

        [Inject]
        public MainMenuPresenter(IMainMenuView mainMenuView)
        {
            _mainMenuView = mainMenuView;
        }

        public void SetInteractable(bool interactable) => _mainMenuView.SetInteractable(interactable);
        public void SetStartVisible(bool visible) => _mainMenuView.SetStartVisible(visible);
    }
}
