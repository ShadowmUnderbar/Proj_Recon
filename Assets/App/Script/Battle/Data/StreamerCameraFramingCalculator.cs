using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 被写体群を画角に収めるための中心・半径・必要距離を求める計算だけを持つ。
    /// </summary>
    public class StreamerCameraFramingCalculator
    {
        /// <summary>自動フレーミングで寄りすぎないための最小距離（m）</summary>
        private const float MinDistance = 2f;

        /// <summary>
        /// 被写体群の重心と、重心から最も遠い被写体までの距離を求める。
        /// 被写体が空の場合はfalseを返す。
        /// </summary>
        public bool TryCalculateBounds(IReadOnlyList<Vector3> subjectPositions, out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = 0f;

            if (subjectPositions == null || subjectPositions.Count == 0)
            {
                return false;
            }

            var sum = Vector3.zero;
            foreach (var position in subjectPositions)
            {
                sum += position;
            }

            center = sum / subjectPositions.Count;

            foreach (var position in subjectPositions)
            {
                radius = Mathf.Max(radius, Vector3.Distance(center, position));
            }

            return true;
        }

        /// <summary>
        /// 半径radiusの被写体群が視野角に収まる水平距離を求める。
        /// 横方向のほうが収まりにくいケースがあるため、縦横それぞれの必要距離のうち大きい方を返す。
        /// </summary>
        public float CalculateFitDistance(
            float radius,
            float verticalFieldOfViewDegrees,
            float aspect,
            float margin,
            float baseDistance
        )
        {
            if (radius <= 0f)
            {
                return baseDistance;
            }

            var verticalHalfRadian = Mathf.Deg2Rad * Mathf.Clamp(verticalFieldOfViewDegrees, 1f, 179f) * 0.5f;
            var verticalTan = Mathf.Tan(verticalHalfRadian);
            if (verticalTan <= Mathf.Epsilon)
            {
                return baseDistance;
            }

            var horizontalTan = verticalTan * Mathf.Max(0.01f, aspect);

            var requiredForVertical = radius / verticalTan;
            var requiredForHorizontal = radius / horizontalTan;
            var required = Mathf.Max(requiredForVertical, requiredForHorizontal) * Mathf.Max(1f, margin);

            return Mathf.Max(MinDistance, Mathf.Max(baseDistance, required));
        }
    }
}
