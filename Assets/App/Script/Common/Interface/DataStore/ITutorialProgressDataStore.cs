using App.Common.Data;

namespace App.Common.Interface
{
    /// <summary>
    /// チュートリアルの閲覧進行。種類ごとの閲覧回数を永続化し、表示要否を判定する。
    /// </summary>
    public interface ITutorialProgressDataStore
    {
        /// <summary>指定チュートリアルの閲覧回数</summary>
        int GetViewCount(TutorialType type);

        /// <summary>規定回数まで閲覧済みか（閲覧済みフラグ）</summary>
        bool IsCompleted(TutorialType type);

        /// <summary>
        /// 今このチュートリアルを表示すべきか。
        /// 未完了なら true。完了済みでも設定で再表示が有効なら true
        /// </summary>
        bool ShouldShow(TutorialType type);

        /// <summary>閲覧を1回分記録して保存する</summary>
        void MarkViewed(TutorialType type);

        /// <summary>指定チュートリアルの閲覧記録を消す（デバッグ・設定のリセット用）</summary>
        void ResetProgress(TutorialType type);
    }
}
