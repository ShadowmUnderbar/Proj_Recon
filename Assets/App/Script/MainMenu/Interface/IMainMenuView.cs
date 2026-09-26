using R3;

namespace App.MainMenu.Interface
{
    /// <summary>
    /// メインメニュー画面のUI。STARTボタンとオプションを開くボタン、
    /// メインパネル／オプションパネルの表示切替を持つ。
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>「START」ボタン押下</summary>
        Observable<Unit> OnStart { get; }

        /// <summary>「OPTION」ボタン押下</summary>
        Observable<Unit> OnOption { get; }

        /// <summary>ボタン操作の可否。遷移中の二重押しを防ぐために使う</summary>
        void SetInteractable(bool interactable);

        /// <summary>メインパネルを表示し、オプションパネルを隠す</summary>
        void ShowMainPanel();

        /// <summary>オプションパネルを表示し、メインパネルを隠す</summary>
        void ShowOptionPanel();
    }
}
