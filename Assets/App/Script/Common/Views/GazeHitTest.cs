using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 視線（頭の正面へのレイ）が対象の箱に当たっているかの幾何判定。状態を持たない計算だけを置く。
    /// ちらつきを抑える判定の持続・遅延は <see cref="GazeDetector"/> が担う
    /// </summary>
    public static class GazeHitTest
    {
        /// <summary>余白角度の上限[度]。90度に近づくと tan が発散するため手前で止める</summary>
        private const float MaxMarginAngle = 89f;

        /// <summary>レイの方向成分がこれ以下なら、その軸と平行とみなす</summary>
        private const float ParallelEpsilon = 1e-6f;

        /// <summary>
        /// 視線が箱に当たっているか。箱は視線から見て marginAngle[度] ぶん外側まで広げて判定する。
        /// 余白を角度で持つのは、近くでも遠くでも「視線がどれだけ外れたら外れとするか」の感覚をそろえるため。
        /// 頭の後ろにある箱は当たらない。頭が箱の中にあるときは当たりとする
        /// </summary>
        public static bool Intersects(in Pose gaze, in GazeTargetBox target, float marginAngle)
        {
            // 箱のローカル座標へ移し、軸に沿った箱とレイの交差（スラブ法）で判定する
            var inverseRotation = Quaternion.Inverse(target.Pose.rotation);
            var origin = inverseRotation * (gaze.position - target.Pose.position);
            var direction = inverseRotation * gaze.forward;

            var margin = GetMarginLength(origin.magnitude, marginAngle);
            // 左右反転などで大きさが負でも、余白が箱を縮める向きに効かないよう絶対値で扱う
            var size = target.Size;
            var halfExtents = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * 0.5f
                + Vector3.one * margin;

            var tMin = 0f;
            var tMax = float.PositiveInfinity;

            for (var axis = 0; axis < 3; axis++)
            {
                if (!ClipSlab(origin[axis], direction[axis], halfExtents[axis], ref tMin, ref tMax))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>距離 distance の位置で、角度 marginAngle[度] が占める長さ[m]</summary>
        private static float GetMarginLength(float distance, float marginAngle)
        {
            var angle = Mathf.Clamp(marginAngle, 0f, MaxMarginAngle);
            return distance * Mathf.Tan(angle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 1軸ぶんの板（-halfExtent〜+halfExtent）でレイの通過区間 [tMin, tMax] を狭める。区間が無くなれば false
        /// </summary>
        private static bool ClipSlab(float origin, float direction, float halfExtent, ref float tMin, ref float tMax)
        {
            // 軸と平行なら、始点が板の内側にあるかどうかだけで決まる
            if (Mathf.Abs(direction) <= ParallelEpsilon)
            {
                return Mathf.Abs(origin) <= halfExtent;
            }

            var t1 = (-halfExtent - origin) / direction;
            var t2 = (halfExtent - origin) / direction;
            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
            }

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            return tMin <= tMax;
        }
    }
}
