using App.Battle.Data;

namespace App.Battle.Interface
{
    /// <summary>
    /// バトル中に出すチュートリアルメッセージの表示。
    /// 表示直後は視点の正面へ追従し、一定時間後に非利き手の脇へ移って常にプレイヤーの方を向く
    /// </summary>
    public interface ITutorialMessageView
    {
        /// <summary>現在の配置フェーズ（デバッグ・検証用）</summary>
        TutorialMessagePhase Phase { get; }

        /// <summary>本文を差し替えて表示する。表示中に呼ばれた場合も視点正面からやり直す</summary>
        void Show(string text);

        /// <summary>表示中の本文だけ差し替える（ロケール切替時用。配置はそのまま）</summary>
        void SetText(string text);

        /// <summary>追従先の姿勢を更新する。表示中に毎フレーム呼ぶ</summary>
        void UpdateAnchor(TutorialMessageAnchor anchor);

        void Hide();
    }
}
