using App.Common.Data;
using App.Common.Views;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// アップグレードカードのボードとカードの置き場所を決める計算。
    /// 状態を持たない純粋な計算だけを置き、ボードの見た目の調整をここで完結させる
    /// </summary>
    public static class UpgradeCardBoardLayout
    {
        /// <summary>
        /// ボード自体をカメラ前方の定位置へ置くための姿勢。俯角ぶん傾けた向きへ配置し、その向きに正対させる
        /// （<see cref="VrUiFollowCanvasView"/> と同じ考え方だが、こちらは開いた瞬間に固定する）
        /// </summary>
        /// <param name="cameraTransform">基準にするカメラ（VRではHMD）</param>
        /// <param name="distance">カメラからボードまでの距離[m]</param>
        /// <param name="pitchAngle">俯角[deg]。0で視線の正面、正の値で下側</param>
        public static Pose CalcBoardPose(Transform cameraTransform, float distance, float pitchAngle)
        {
            var yawForward = Quaternion.AngleAxis(-pitchAngle, cameraTransform.right) * cameraTransform.forward;
            yawForward.y = 0f;

            // 想定した俯角から大きく外れて水平成分が消えたときは、頭の上方向を代わりの基準にする
            if (yawForward.sqrMagnitude <= VectorConstants.DirectionEpsilon)
            {
                yawForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            }

            if (yawForward.sqrMagnitude <= VectorConstants.DirectionEpsilon)
            {
                yawForward = Vector3.forward;
            }

            yawForward.Normalize();

            var right = Vector3.Cross(Vector3.up, yawForward);
            var pitchRotation = Quaternion.AngleAxis(pitchAngle, right.normalized);
            var direction = pitchRotation * yawForward;
            var up = pitchRotation * Vector3.up;

            return new Pose(
                cameraTransform.position + direction * distance,
                Quaternion.LookRotation(direction, up));
        }

        /// <summary>
        /// 非VRでのボードの姿勢。見下ろしの固定アングルでは俯角で置くと視界の外へ出てしまうため、
        /// <see cref="VrUiFollowCanvasView"/> の非VR時と同じく、カメラ相対の一定位置に貼り付ける
        /// </summary>
        /// <param name="cameraTransform">基準にするカメラ</param>
        /// <param name="localPosition">カメラ相対のボード位置[m]</param>
        public static Pose CalcPointerBoardPose(Transform cameraTransform, Vector3 localPosition)
        {
            return new Pose(cameraTransform.TransformPoint(localPosition), cameraTransform.rotation);
        }

        /// <summary>ボード内でのカードの位置。左右・上下の中央揃えで格子状に並べる</summary>
        /// <param name="index">カードの並び順</param>
        /// <param name="totalCount">カードの総数</param>
        /// <param name="columnCount">1行あたりのカード枚数</param>
        /// <param name="cardSpacing">カードの間隔[m]（X:横 Y:縦）</param>
        public static Vector3 CalcCardLocalPosition(int index, int totalCount, int columnCount, Vector2 cardSpacing)
        {
            columnCount = Mathf.Max(1, columnCount);
            var rowCount = Mathf.CeilToInt((float)totalCount / columnCount);
            var row = index / columnCount;
            var column = index % columnCount;

            // 最終行は枚数が欠けることがあるため、その行の枚数で中央揃えする
            var countInRow = Mathf.Min(columnCount, totalCount - row * columnCount);

            var x = (column - (countInRow - 1) * 0.5f) * cardSpacing.x;
            var y = -(row - (rowCount - 1) * 0.5f) * cardSpacing.y;

            return new Vector3(x, y, 0f);
        }
    }
}
