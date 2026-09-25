using R3;

namespace App.MainMenu.Interface
{
    public interface IMainMenuPresenter
    {
        /// <summary>「START」ボタン押下</summary>
        Observable<Unit> OnStart { get; }

        void SetInteractable(bool interactable);
    }
}
