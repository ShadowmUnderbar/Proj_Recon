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

        // 発射地点側からラインを収縮させて消す速度（ワールド単位/秒）。距離に依らず一定速度で消す
        private const float RetractSpeed = 200f;

        /// <summary>
        /// トレーサーを再生する。
        /// </summary>
        /// <param name="startPos">発射地点</param>
        /// <param name="endPos">着弾地点</param>
        /// <param name="material">ラインに使用するマテリアル（元の弾と揃える。nullならプレハブ設定のまま）</param>
        public async UniTask Play(Vector3 startPos, Vector3 endPos, Material material)
        {
            // 元の弾と同じマテリアルを使用
            if (material != null)
            {
                lineRenderer.sharedMaterial = material;
            }

            // 即座に発射地点→着弾地点の直線を描画
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            // 一瞬保持
            await UniTask.WaitForSeconds(HoldDuration);

            // 発射地点側の端点を一定速度で着弾地点へ寄せ、発射地点側から消していく
            var current = startPos;
            while (current != endPos)
            {
                current = Vector3.MoveTowards(current, endPos, RetractSpeed * Time.deltaTime);
                lineRenderer.SetPosition(0, current);
                await UniTask.Yield();
            }

            Destroy(gameObject);
        }
    }
}
