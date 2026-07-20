using R3;

namespace App.Battle.Interface
{
    public interface IRunStartPresenter
    {
        /// <summary>スロット選択（選ばれたスロット番号 0〜2）</summary>
        Observable<int> OnSlotSelected { get; }

        /// <summary>「使わずに開始」ボタン押下</summary>
        Observable<Unit> OnStartWithoutLoad { get; }

        void Show(string headline);
        void SetSlot(int index, string label, bool interactable);
        void Hide();
    }
}
