using R3;

namespace App.MainMenu.Interface
{
    /// <summary>
    /// メインメニュー画面のUI。タイトルと「START」ボタンを持つ。
    /// START後のアップグレードセット選択は同じパネル内の RunStartView が担当する。
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>「START」ボタン押下</summary>
        Observable<Unit> OnStart { get; }

        /// <summary>ボタン操作の可否。遷移中の二重押しを防ぐために使う</summary>
        void SetInteractable(bool interactable);

        /// <summary>「START」ボタンの表示切替。セット選択中は隠す</summary>
        void SetStartVisible(bool visible);
    }
}
