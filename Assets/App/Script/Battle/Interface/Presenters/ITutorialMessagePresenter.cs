using App.Battle.Data;

namespace App.Battle.Interface
{
    /// <summary>
    /// チュートリアルメッセージ表示への窓口
    /// </summary>
    public interface ITutorialMessagePresenter
    {
        /// <summary>現在の配置フェーズ（デバッグ・検証用）</summary>
        TutorialMessagePhase Phase { get; }

        /// <summary>本文を差し替えて表示する。表示中でも視点正面からやり直す</summary>
        void Show(string text);

        /// <summary>表示中の本文だけ差し替える</summary>
        void SetText(string text);

        /// <summary>追従先の姿勢を更新する</summary>
        void UpdateAnchor(TutorialMessageAnchor anchor);

        void Hide();
    }
}
