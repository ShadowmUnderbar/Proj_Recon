using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    /// <summary>
    /// 中心（プレイヤー）から一定距離の内側へ入らずに目的地へ向かうための、次の行き先を決める。
    /// 目的地が内側なら外周へ押し出し、まっすぐ向かうと内側を横切るときは、外周（少し余裕を持たせた円）を経由する。
    /// 円の外にいれば円への接点へ向かい（最短で回り込む）、円の上にいれば円に沿って少し先の点へ向かう。
    /// 毎フレーム呼び直すことで、外周を円弧状に回り込む。高さは目的地のものを使い、距離は水平面で測る
    /// </summary>
    public static class KeepDistanceRoute
    {
        /// <summary>1回に外周を回り込む角度[度]。弦が内側へ食い込む量は 半径×(1-cos(角度/2)) なので、余裕と合わせて内側に入らない大きさにする</summary>
        private const float ArcStepDegrees = 40f;

        /// <summary>回り込むときの半径の余裕（一定距離に対する倍率）。弦のたわみ（cos(20度)≒0.94）を打ち消して内側へ入らないようにする</summary>
        private const float DetourRadiusMargin = 1.1f;

        /// <summary>中心と重なったときに使う向き（どちらへ出ても良いので固定）</summary>
        private static readonly Vector2 FallbackDirection = Vector2.right;

        /// <param name="self">今いる位置</param>
        /// <param name="center">近づかない中心（プレイヤー）</param>
        /// <param name="goal">本来の目的地</param>
        /// <param name="keepDistance">中心から保つ距離[m]</param>
        /// <returns>今向かうべき行き先</returns>
        public static Vector3 NextDestination(Vector3 self, Vector3 center, Vector3 goal, float keepDistance)
        {
            var toSelf = Flatten(self - center);
            var toGoal = Flatten(goal - center);

            // 目的地そのものが内側なら外周まで押し出す
            if (toGoal.sqrMagnitude < keepDistance * keepDistance)
            {
                toGoal = SafeNormalize(toGoal, SafeNormalize(toSelf, FallbackDirection)) * keepDistance;
            }

            var destination = ToWorld(center, toGoal, goal.y);

            // まっすぐ向かっても内側を通らないなら、そのまま向かう
            if (SegmentDistanceToOrigin(toSelf, toGoal) >= keepDistance)
            {
                return destination;
            }

            // 内側を横切る：回り込む円（保つ距離＋余裕）を経由して、目的地の側へ回る
            var selfDirection = SafeNormalize(toSelf, SafeNormalize(toGoal, FallbackDirection));
            var angle = Vector2.SignedAngle(selfDirection, toGoal);
            var radius = keepDistance * DetourRadiusMargin;
            var distance = toSelf.magnitude;

            // 円の外にいれば、円への接点まで回す（その直線は円に接するだけで内側を通らない）。
            // 円の上・内側にいれば、円に沿って少しずつ回す
            var maxStep = distance > radius
                ? Mathf.Acos(radius / distance) * Mathf.Rad2Deg
                : ArcStepDegrees;
            var step = Mathf.Clamp(angle, -maxStep, maxStep);
            return ToWorld(center, Rotate(selfDirection, step) * radius, goal.y);
        }

        /// <summary>
        /// 中心から <paramref name="keepDistance"/> の内側にいるとき、外周まで押し戻す移動量と、外向きの単位ベクトルを返す。
        /// エージェントは速度の慣性で行き先の外周より内側へ切り込むことがあるので、移動のあとにこれで補正する
        /// </summary>
        /// <returns>内側にいて押し戻しが要るなら true</returns>
        public static bool TryGetPushOut(Vector3 self, Vector3 center, float keepDistance, out Vector3 pushOut, out Vector3 outward)
        {
            var offset = Flatten(self - center);
            var distance = offset.magnitude;
            if (distance >= keepDistance || distance < Mathf.Epsilon)
            {
                pushOut = Vector3.zero;
                outward = Vector3.zero;
                return false;
            }

            var direction = offset / distance;
            outward = new Vector3(direction.x, 0f, direction.y);
            pushOut = outward * (keepDistance - distance);
            return true;
        }

        /// <summary>線分 a→b と原点の最短距離</summary>
        public static float SegmentDistanceToOrigin(Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSqr = ab.sqrMagnitude;
            if (lengthSqr < Mathf.Epsilon)
            {
                return a.magnitude;
            }

            var t = Mathf.Clamp01(-Vector2.Dot(a, ab) / lengthSqr);
            return (a + ab * t).magnitude;
        }

        private static Vector2 Flatten(Vector3 v) => new(v.x, v.z);

        private static Vector3 ToWorld(Vector3 center, Vector2 offset, float y) => new(center.x + offset.x, y, center.z + offset.y);

        private static Vector2 SafeNormalize(Vector2 v, Vector2 fallback) => v.sqrMagnitude > Mathf.Epsilon ? v.normalized : fallback;

        /// <summary>XZ平面のベクトルを、上から見た Vector2.SignedAngle と同じ向きに回す</summary>
        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
