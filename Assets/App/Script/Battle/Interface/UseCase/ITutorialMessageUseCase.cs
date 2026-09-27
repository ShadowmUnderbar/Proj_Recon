using App.Common.Data;

namespace App.Battle.Interface
{
    /// <summary>
    /// バトル中にチュートリアルメッセージを出す手段。表示のきっかけを持つ側がこれを注入して呼ぶ
    /// </summary>
    public interface ITutorialMessageUseCase
    {
        /// <summary>指定種類のメッセージを表示する。表示中でも差し替えて視点正面からやり直す</summary>
        void Show(TutorialType type);

        /// <summary>メッセージを消す</summary>
        void Hide();
    }
}
