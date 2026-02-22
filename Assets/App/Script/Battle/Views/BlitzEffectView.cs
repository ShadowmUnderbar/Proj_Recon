using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Views
{
    public class BlitzEffectView : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        private const float StartDuration = 0.1f;
        private const float WaitDuration = 1.5f;
        private const float DelayDuration = 0.5f;
        private Vector3 OffsetPosition => new Vector3(0f, 1f, 0f);

        public async UniTask Play(Vector3 startPos, Transform playerTransform)
        {
            startPos += OffsetPosition;

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, startPos);

            var time = 0f;
            while (time < StartDuration)
            {
                lineRenderer.SetPosition(1,
                    Vector3.Lerp(startPos, playerTransform.position + OffsetPosition, time / StartDuration));
                time += Time.deltaTime;
                await UniTask.Yield();
            }

            time = 0f;
            while (time < WaitDuration)
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

            while (time < DelayDuration)
            {
                time += Time.deltaTime;
                var progress = Mathf.Clamp01(time / DelayDuration);
                var startIndex = Mathf.Min(Mathf.FloorToInt(progress * finalPointCount), finalPointCount - 1);

                if (startIndex >= finalPointCount - 1) break;

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