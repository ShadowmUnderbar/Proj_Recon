using R3;

namespace App.Common.Interface
{
    public interface IRunStartPresenter
    {
        /// <summary>スロット選択（選ばれたスロット番号 0〜2）</summary>
        Observable<int> OnSlotSelected { get; }

        /// <summary>「使わずに開始」ボタン押下</summary>
        Observable<Unit> OnStartWithoutLoad { get; }

        /// <summary>「戻る」ボタン押下。ボタンを持たない画面では流れない</summary>
        Observable<Unit> OnBack { get; }

        void Show(string headline);
        void SetSlot(int index, string label, bool interactable);
        void Hide();
    }
}
