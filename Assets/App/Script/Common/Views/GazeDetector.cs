using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// 対象を視線で見ているかの判定（plain C#、DI 対象外）。対象 1 つにつき 1 インスタンス持つ。
    /// 幾何判定は <see cref="GazeHitTest"/> に任せ、ここでは境目でのちらつきを抑える。
    /// ・見ている間は外れの余白角度を広げる（ヒステリシス）
    /// ・当たり／外れが規定時間続いてから切り替える
    /// 見た目の大きさが判定で変わる対象（見たら拡大する等）でも、判定の半径は固定で渡すと揺れない
    /// </summary>
    public class GazeDetector
    {
        /// <summary>対象を見ているか（遅延・ヒステリシス適用後）</summary>
        public bool IsGazed { get; private set; }

        /// <summary>今と逆の判定が続いている時間[s]。切り替わるか元に戻ったら 0 に戻す</summary>
        private float _pendingTime;

        /// <summary>
        /// 視線と対象を更新し、見ているかを返す。
        /// deltaTime は前回この判定を呼んでからの経過時間。毎フレーム呼ばない場合もフレーム時間ではなくこちらを渡す。
        /// 視線が取れないフレームは呼ばない（判定を据え置く）か、<see cref="Reset"/> で見ていない状態へ戻す
        /// </summary>
        public bool Update(
            float deltaTime, in Pose gaze, Vector3 center, float radius, in GazeDetectorSettings settings)
        {
            var marginAngle = IsGazed
                ? Mathf.Max(settings.EnterMarginAngle, settings.ExitMarginAngle)
                : settings.EnterMarginAngle;
            var isHit = GazeHitTest.Intersects(gaze, center, radius, marginAngle);

            if (isHit == IsGazed)
            {
                _pendingTime = 0f;
                return IsGazed;
            }

            _pendingTime += deltaTime;
            var delay = IsGazed ? settings.ExitDelay : settings.EnterDelay;
            if (_pendingTime >= delay)
            {
                IsGazed = isHit;
                _pendingTime = 0f;
            }

            return IsGazed;
        }

        /// <summary>見ていない状態へ戻す。表示し直すときなどに呼ぶ</summary>
        public void Reset()
        {
            IsGazed = false;
            _pendingTime = 0f;
        }
    }
}
