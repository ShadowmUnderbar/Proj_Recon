using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 注視判定の対象となる箱（ワールド空間、単位はm）。
    /// 奥行き 0 の箱にすれば、UI パネルのような平面の矩形として扱える
    /// </summary>
    public readonly struct GazeTargetBox
    {
        /// <summary>箱の中心の位置と向き</summary>
        public readonly Pose Pose;

        /// <summary>箱の大きさ[m]。Pose のローカル軸（x:右 y:上 z:前）に沿った幅・高さ・奥行き</summary>
        public readonly Vector3 Size;

        public GazeTargetBox(Pose pose, Vector3 size)
        {
            Pose = pose;
            Size = size;
        }

        /// <summary>
        /// RectTransform の矩形（奥行き 0）から作る。スケールは lossyScale を反映する。
        /// pivot が中央でなくても、矩形の実際の中心を箱の中心にする
        /// </summary>
        public static GazeTargetBox FromRectTransform(RectTransform rectTransform)
        {
            var rect = rectTransform.rect;
            var scale = rectTransform.lossyScale;
            var center = rectTransform.TransformPoint(rect.center);
            var size = new Vector3(rect.width * scale.x, rect.height * scale.y, 0f);

            return new GazeTargetBox(new Pose(center, rectTransform.rotation), size);
        }
    }
}
