using App.Common.Data;

namespace App.Common.Interface
{
    /// <summary>
    /// チュートリアルメッセージを出す手段（バトル・メインメニュー共通）。表示のきっかけを持つ側がこれを注入して呼ぶ
    /// </summary>
    public interface ITutorialMessageUseCase
    {
        /// <summary>指定種類のメッセージを表示する。表示中でも差し替えて視点正面からやり直す</summary>
        void Show(TutorialType type);

        /// <summary>
        /// 閲覧済み（規定回数）でなければ、または再表示設定が有効なら表示し、閲覧回数を記録する。
        /// 出さなかった場合は何もしない（表示中のメッセージも消さない）
        /// </summary>
        /// <returns>表示したか</returns>
        bool ShowIfNeeded(TutorialType type);

        /// <summary>メッセージを消す</summary>
        void Hide();
    }
}
