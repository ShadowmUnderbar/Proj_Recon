using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 視線（頭の正面へのレイ）が対象に当たっているかの幾何判定。状態を持たない計算だけを置く。
    /// 対象は球（中心＋半径）で近似する。毎フレーム多数の対象を判定しても負担にならないよう、
    /// 平方根や回転の逆算を使わず内積と比較だけで済ませる。
    /// ちらつきを抑える判定の持続・遅延は <see cref="GazeDetector"/> が担う
    /// </summary>
    public static class GazeHitTest
    {
        /// <summary>余白角度の上限[度]。90度に近づくと tan が発散するため手前で止める</summary>
        private const float MaxMarginAngle = 89f;

        /// <summary>
        /// 視線が球に当たっているか。球は視線から見て marginAngle[度] ぶん外側まで広げて判定する。
        /// 余白を角度で持つのは、近くでも遠くでも「視線がどれだけ外れたら外れとするか」の感覚をそろえるため。
        /// 頭の後ろにある球は当たらない。頭が球の中にあるときは当たりとする
        /// </summary>
        public static bool Intersects(in Pose gaze, Vector3 center, float radius, float marginAngle)
        {
            radius = Mathf.Max(radius, 0f);

            var toCenter = center - gaze.position;
            var sqrDistance = toCenter.sqrMagnitude;
            if (sqrDistance <= radius * radius)
            {
                return true;
            }

            // 視線方向に沿った中心までの距離。負なら頭の後ろ
            var along = Vector3.Dot(toCenter, gaze.forward);
            if (along <= 0f)
            {
                return false;
            }

            // 視線から中心までの距離の二乗（三平方）と、余白ぶん広げた半径の二乗を比べる
            var angle = Mathf.Clamp(marginAngle, 0f, MaxMarginAngle);
            var hitRadius = radius + along * Mathf.Tan(angle * Mathf.Deg2Rad);
            return sqrDistance - along * along <= hitRadius * hitRadius;
        }
    }
}
