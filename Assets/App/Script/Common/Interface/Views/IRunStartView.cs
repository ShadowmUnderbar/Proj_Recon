using R3;

namespace App.Common.Interface
{
    /// <summary>
    /// ラン開始前のセット選択UI。保存済みスロットを選んで最初から装備するか、使わずに開始する。
    /// メインメニュー（START後）とバトルシーン（直接再生時のフォールバック）の両方で使う。
    /// </summary>
    public interface IRunStartView
    {
        /// <summary>スロット選択（選ばれたスロット番号 0〜2）</summary>
        Observable<int> OnSlotSelected { get; }

        /// <summary>「使わずに開始」ボタン押下</summary>
        Observable<Unit> OnStartWithoutLoad { get; }

        /// <summary>「戻る」ボタン押下。ボタンを持たない画面（バトル側）では流れない</summary>
        Observable<Unit> OnBack { get; }

        /// <summary>画面を表示し、見出しを設定する</summary>
        void Show(string headline);

        /// <summary>指定スロットのラベルと押下可否を設定する（空スロットは押下不可）</summary>
        void SetSlot(int index, string label, bool interactable);

        void Hide();
    }
}
