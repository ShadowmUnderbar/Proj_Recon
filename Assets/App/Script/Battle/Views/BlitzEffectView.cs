using App.Common.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Views
{
    public class BlitzEffectView : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        private const float StartDuration = 0.1f;
        private const float WaitDuration = 0.2f;
        private Vector3 OffsetPosition => new(0f, 1f, 0f);

        public async UniTask Play(Vector3 startPos, Transform playerTransform)
        {
            startPos += OffsetPosition;

            // 伸びていく直線部分。カーブ有効時はここだけ刻む。
            // このあと追加される軌跡の点は毎フレーム打たれるので元から細かい
            CurvedWorldLine.SetLine(lineRenderer, startPos, startPos);

            var time = 0f;
            while (time < StartDuration)
            {
                var head = Vector3.Lerp(startPos, playerTransform.position + OffsetPosition, time / StartDuration);
                CurvedWorldLine.SetLine(lineRenderer, startPos, head);
                time += Time.deltaTime;
                await UniTask.Yield();
            }

            time = 0f;
            while (time < WaitDuration * 0.5f)
            {
                time += Time.deltaTime;
                await UniTask.Yield();
                lineRenderer.positionCount += 1;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, playerTransform.position + OffsetPosition);
            }

            var finalPointCount = lineRenderer.positionCount;
            var allPositions = new Vector3[finalPointCount];
            lineRenderer.GetPositions(allPositions);
            var tempBuffer = new Vector3[finalPointCount];

            var prevStartIndex = 0;
            time = 0f;

            while (time < WaitDuration * 0.5f)
            {
                time += Time.deltaTime;
                var progress = Mathf.Clamp01(time / WaitDuration * 0.5f);
                var startIndex = Mathf.Min(Mathf.FloorToInt(progress * finalPointCount), finalPointCount - 1);

                if (startIndex >= finalPointCount - 1)
                {
                    break;
                }

                if (startIndex != prevStartIndex)
                {
                    prevStartIndex = startIndex;
                    var remainingCount = finalPointCount - startIndex;
                    System.Array.Copy(allPositions, startIndex, tempBuffer, 0, remainingCount);
                    lineRenderer.positionCount = remainingCount;
                    lineRenderer.SetPositions(tempBuffer);
                }

                await UniTask.Yield();
            }

            Destroy(gameObject);
        }
    }
}