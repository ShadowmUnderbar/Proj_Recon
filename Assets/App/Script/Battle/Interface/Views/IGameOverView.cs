using R3;

namespace App.Battle.Interface
{
    /// <summary>
    /// ゲームオーバー画面のUI。スロット3つへの保存ボタン・リスタートボタン・
    /// メインメニューへ戻るボタンと状態テキストを持つ。
    /// </summary>
    public interface IGameOverView
    {
        /// <summary>保存スロットボタン押下（押されたスロット番号 0〜2）</summary>
        Observable<int> OnSaveSlotSelected { get; }

        /// <summary>「リスタート」ボタン押下</summary>
        Observable<Unit> OnRestart { get; }

        /// <summary>「メインメニューへ」ボタン押下</summary>
        Observable<Unit> OnReturnToMainMenu { get; }

        /// <summary>画面を表示し、見出しを設定する</summary>
        void Show(string headline);

        /// <summary>指定スロットボタンのラベルを設定する</summary>
        void SetSlotLabel(int index, string label);

        /// <summary>状態テキスト（保存結果など）を設定する</summary>
        void SetStatus(string status);

        /// <summary>画面を隠す</summary>
        void Hide();
    }
}
