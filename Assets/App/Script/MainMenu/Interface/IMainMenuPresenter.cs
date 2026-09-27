using R3;

namespace App.MainMenu.Interface
{
    public interface IMainMenuPresenter
    {
        /// <summary>「START」ボタン押下</summary>
        Observable<Unit> OnStart { get; }

        void SetInteractable(bool interactable);

        /// <summary>「START」ボタンの表示切替。セット選択中は隠す</summary>
        void SetStartVisible(bool visible);
    }
}
