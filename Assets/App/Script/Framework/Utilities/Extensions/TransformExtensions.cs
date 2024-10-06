using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Framework.Utilities.Extensions
{
    public static class TransformExtensions
    {
        public static Pose ToPose(this Transform transform)
        {
            return new(transform.position, transform.rotation);
        }

        public static T InstantiateChildInSamePlace<T>(this Transform transform, T prefab) where T : Component
        {
            var go = Object.Instantiate(prefab, transform.position, transform.rotation);
            go.transform.SetParent(transform);

            return go;
        }

        public static void SetChildWithMoveSamePlace(this Transform transform, Transform targetTransform)
        {
            targetTransform.SetParent(transform);
            targetTransform.localPosition = Vector3.zero;
            targetTransform.localRotation = Quaternion.identity;
        }

        public static async UniTaskVoid Shake(this Transform transform, float entireDuration, CancellationToken ct,
            float strength = 1f,
            int vibrato = 10,
            float randomness = 90f)
        {
            var iterationCount = (int)(vibrato * entireDuration);
            if (iterationCount < 2)
            {
                iterationCount = 2;
            }

            var decayStrength = strength / iterationCount;
            var time = 0f;
            var pos = transform.localPosition;
            var originalPos = pos;
            var angle = Random.Range(0f, 360f);
            var iterationDuration = entireDuration / (iterationCount - 1);
            for (var i = 0; i < iterationCount; ++i)
            {
                angle = angle - 180 + Random.Range(-randomness, randomness);

                var nextPos = originalPos + Quaternion.AngleAxis(Random.Range(-randomness, randomness), Vector3.up) *
                    new Vector3(
                        strength * Mathf.Cos(Mathf.Deg2Rad * angle),
                        strength * Mathf.Sin(Mathf.Deg2Rad * angle),
                        0
                    );

                if (i == iterationCount - 1)
                {
                    nextPos = originalPos;
                }

                var duration = i == 0 || i == iterationCount - 1 ? iterationDuration / 2 : iterationDuration;
                var baseTime = i == 0 ? 0 : iterationDuration * (i - 0.5f);
                while (time < baseTime + duration)
                {
                    var rate = (time - baseTime) / duration;
                    transform.localPosition = Vector3.Lerp(pos, nextPos, rate);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    if (ct.IsCancellationRequested)
                    {
                        transform.localPosition = originalPos;
                        return;
                    }

                    time += Time.deltaTime;
                }

                strength -= decayStrength;

                pos = nextPos;
            }
        }

        public static void RotateYAxis(this Transform transform, float angle) => transform.Rotate(Vector3.up, angle);
    }
}