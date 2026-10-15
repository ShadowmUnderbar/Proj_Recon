using UnityEngine;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// サモンドローン。プレイヤーの後ろ上方に浮かんで追従し、射程内で最も近い敵へ砲身を向けて撃つ
    /// （狙う・撃つ処理は <see cref="AllyGunViewBase"/>）。
    /// プレイヤーの視界・射線を塞がないよう、プレイヤーの向き（射撃の向き）の後ろ側に扇状に並ぶ
    /// </summary>
    public class AllyDroneView : AllyGunViewBase
    {
        [SerializeField, Min(0f), Tooltip("プレイヤーの足元からの浮かぶ高さ[m]。敵の弾（水平に飛ぶ）が当たる胴の高さにする")]
        private float _hoverHeight = 1.4f;

        [SerializeField, Min(0f), Tooltip("プレイヤーから水平にどれだけ離れて並ぶか[m]")]
        private float _followDistance = 1.6f;

        [SerializeField, Range(0f, 180f), Tooltip("並ぶ扇の広さ[度]。プレイヤーの真後ろを中心に左右へ広げ、ドローンの数で等分する")]
        private float _slotSpreadAngle = 120f;

        [SerializeField, Min(0.01f), Tooltip("追従の遅れ[秒]。小さいほどきびきび付いてくる")]
        private float _followSmoothTime = 0.25f;

        private Vector3 _followVelocity;

        protected override void UpdateMovement()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, GetFollowPosition(), ref _followVelocity,
                _followSmoothTime, AllyData.MoveSpeed);
        }

        /// <summary>
        /// プレイヤーの向きの後ろ側の扇のうち、このドローンの位置。
        /// 扇をドローンの数で等分し、出現順（AllyStoreView が配る番号）に並べる。1体なら真後ろ
        /// </summary>
        private Vector3 GetFollowPosition()
        {
            // プレイヤーのルートは回転しないため、射撃の向き（AllyUseCase が渡す）を使う
            var back = -PlayerForward;
            back.y = 0f;
            back = back.sqrMagnitude > 0f ? back.normalized : Vector3.back;

            var t = FormationCount <= 1 ? 0.5f : (float)FormationIndex / (FormationCount - 1);
            var angle = Mathf.Lerp(-_slotSpreadAngle * 0.5f, _slotSpreadAngle * 0.5f, t);
            var direction = Quaternion.AngleAxis(angle, Vector3.up) * back;

            return PlayerTransform.position + direction * _followDistance + Vector3.up * _hoverHeight;
        }
    }
}
