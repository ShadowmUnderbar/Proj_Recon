using App.Battle.Data;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// チュートリアルメッセージの配置計算（plain C#、DI 対象外）。
    /// 表示直後は視点の正面へ追従し、規定時間を過ぎたら非利き手の脇へ移り、以降は常に頭の方を向く。
    /// 手の姿勢が使えない（非VR）間は視点の正面に留まり続ける。
    ///
    /// 目標位置はフェーズごとに毎フレーム決め直し、実際の姿勢はそこへ指数補間で追いつかせる。
    /// フェーズ切り替えの瞬間に跳ばず、手ぶれもそのまま伝わらないようにするため
    /// </summary>
    public class TutorialMessagePlacement
    {
        public TutorialMessagePhase Phase { get; private set; } = TutorialMessagePhase.Hidden;

        /// <summary>表示開始からの経過時間[s]</summary>
        private float _elapsed;

        /// <summary>表示後に一度でも配置したか。最初の1回は補間せず目標へスナップする</summary>
        private bool _isPlaced;

        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;

        /// <summary>表示を開始し、視点正面フェーズからやり直す</summary>
        public void Begin()
        {
            Phase = TutorialMessagePhase.HeadFollow;
            _elapsed = 0f;
            _isPlaced = false;
        }

        public void End()
        {
            Phase = TutorialMessagePhase.Hidden;
        }

        /// <summary>
        /// 追従先を更新して今フレームの姿勢を返す。非表示中は false
        /// </summary>
        public bool TryUpdate(
            float deltaTime, in TutorialMessageAnchor anchor, in TutorialMessagePlacementSettings settings,
            out Pose pose)
        {
            if (Phase == TutorialMessagePhase.Hidden)
            {
                pose = default;
                return false;
            }

            // 経過時間は最初に配置されたフレームから数える。
            // 表示直後のロード待ちや大きなフレーム落ちで、見える前に視点正面の時間を使い切らないようにする
            if (_isPlaced)
            {
                _elapsed += deltaTime;
            }

            Phase = anchor.IsHandAvailable && _elapsed >= settings.HeadFollowDuration
                ? TutorialMessagePhase.HandFollow
                : TutorialMessagePhase.HeadFollow;

            var target = Phase == TutorialMessagePhase.HandFollow
                ? GetHandFollowPose(anchor, settings)
                : GetHeadFollowPose(anchor, settings);

            if (!_isPlaced || settings.FollowSpeed <= 0f)
            {
                _position = target.position;
                _rotation = target.rotation;
                _isPlaced = true;
            }
            else
            {
                // フレームレートに依存しないよう指数補間で追いつかせる
                var t = 1f - Mathf.Exp(-settings.FollowSpeed * deltaTime);
                _position = Vector3.Lerp(_position, target.position, t);
                _rotation = Quaternion.Slerp(_rotation, target.rotation, t);
            }

            pose = new Pose(_position, _rotation);
            return true;
        }

        /// <summary>視点の正面。頭のローカル座標でオフセットし、向きは頭と揃える（正対して読める）</summary>
        private static Pose GetHeadFollowPose(
            in TutorialMessageAnchor anchor, in TutorialMessagePlacementSettings settings)
        {
            var head = anchor.HeadPose;
            return new Pose(head.position + head.rotation * settings.HeadOffset, head.rotation);
        }

        /// <summary>
        /// 非利き手の脇。手のローカル座標でオフセットし、常に頭の方を向ける。
        /// オフセットは左手向けの値として扱い、右手のときは x を反転して鏡写しにする
        /// </summary>
        private static Pose GetHandFollowPose(
            in TutorialMessageAnchor anchor, in TutorialMessagePlacementSettings settings)
        {
            var offset = settings.HandOffset;
            if (anchor.Hand == HandType.Right)
            {
                offset.x = -offset.x;
            }

            var hand = anchor.HandPose;
            var position = hand.position + hand.rotation * offset;

            return new Pose(position, FaceToward(position, anchor.HeadPose));
        }

        /// <summary>
        /// 頭からメッセージへ向く回転（Canvas の前方が視線と同じ向きになり、正面から読める）。
        /// 上方向はワールドではなく頭の上方向を使う。手元を見下ろしたときにメッセージがほぼ真下へ来ても、
        /// 頭の上方向は視線と直交しているため向きが定まり、トラッキングの揺れで回転しない。
        /// それでも定まらない（頭の真上・真下）ときは頭の向きをそのまま使う
        /// </summary>
        private static Quaternion FaceToward(Vector3 position, in Pose head)
        {
            var direction = position - head.position;
            var up = head.rotation * Vector3.up;

            if (Vector3.Cross(direction, up).sqrMagnitude <= VectorConstants.DirectionEpsilon)
            {
                return head.rotation;
            }

            return Quaternion.LookRotation(direction, up);
        }
    }
}
