using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 即着弾（hitscan）の視覚表現。
    /// 発射地点から着弾地点へ一瞬でラインを引き、わずかに保持した後、
    /// 発射地点側からラインを収縮させて消す曳光弾風のエフェクト。
    /// </summary>
    public class BulletTracerView : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        // 発射地点→着弾地点を引いた後、収縮を始めるまでの保持時間
        private const float HoldDuration = 0.05f;

        // 発射地点側からラインを収縮させて消すのにかける時間
        private const float RetractDuration = 0.15f;

        /// <summary>
        /// トレーサーを再生する。
        /// </summary>
        /// <param name="startPos">発射地点</param>
        /// <param name="endPos">着弾地点</param>
        public async UniTask Play(Vector3 startPos, Vector3 endPos)
        {
            // 即座に発射地点→着弾地点の直線を描画
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            // 一瞬保持
            await UniTask.WaitForSeconds(HoldDuration);

            // 発射地点側の端点を着弾地点へ寄せ、発射地点側から消していく
            var time = 0f;
            while (time < RetractDuration)
            {
                var progress = time / RetractDuration;
                lineRenderer.SetPosition(0, Vector3.Lerp(startPos, endPos, progress));
                time += Time.deltaTime;
                await UniTask.Yield();
            }

            Destroy(gameObject);
        }
    }
}
