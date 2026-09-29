using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// チュートリアルメッセージの縮小（1行目だけの表示）と展開の状態（plain C#、DI 対象外）。
    /// 背景の高さは進み具合 <see cref="Progress"/> で補間し、本文の切り替えは次の順にする。
    /// ・縮小: 先に本文を1行目だけにしてから背景を縮める
    /// ・展開: 背景が広がりきってから本文を全文に戻す
    /// どちらも本文が背景からはみ出さないようにするため
    /// </summary>
    public class TutorialMessageFold
    {
        /// <summary>縮小の進み具合。0 で展開しきり、1 で縮小しきり</summary>
        public float Progress { get; private set; }

        /// <summary>本文を1行目だけにするか。縮小を求められている間と、展開しきるまでの間は true</summary>
        public bool IsTextCollapsed { get; private set; }

        /// <summary>
        /// 縮小するかを受け取り、進み具合を進める。duration は縮小・展開にかける時間[s]で、0以下なら即座に切り替える
        /// </summary>
        public void Update(float deltaTime, bool shouldCollapse, float duration)
        {
            var target = shouldCollapse ? 1f : 0f;
            Progress = duration <= 0f ? target : Mathf.MoveTowards(Progress, target, deltaTime / duration);
            IsTextCollapsed = shouldCollapse || Progress > 0f;
        }

        /// <summary>展開しきった状態へ即座に戻す。表示し直すときに呼ぶ</summary>
        public void Reset()
        {
            Progress = 0f;
            IsTextCollapsed = false;
        }
    }
}
