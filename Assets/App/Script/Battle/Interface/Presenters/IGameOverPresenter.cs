using R3;

namespace App.Battle.Interface
{
    public interface IGameOverPresenter
    {
        /// <summary>保存スロットボタン押下（押されたスロット番号 0〜2）</summary>
        Observable<int> OnSaveSlotSelected { get; }

        /// <summary>「リスタート」ボタン押下</summary>
        Observable<Unit> OnRestart { get; }

        void Show(string headline);
        void SetSlotLabel(int index, string label);
        void SetStatus(string status);
        void Hide();
    }
}
