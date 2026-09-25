using R3;

namespace App.MainMenu.Interface
{
    /// <summary>
    /// メインメニュー画面のUI。現時点はバトルを開始するボタンのみ。
    /// オプションや永続強化は後から増やす。
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>「START」ボタン押下</summary>
        Observable<Unit> OnStart { get; }

        /// <summary>ボタン操作の可否。遷移中の二重押しを防ぐために使う</summary>
        void SetInteractable(bool interactable);
    }
}
