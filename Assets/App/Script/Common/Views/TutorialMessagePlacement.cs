using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// チュートリアルメッセージの配置計算（plain C#、DI 対象外）。
    /// 表示直後は視点の正面へ追従し、規定時間を過ぎたら非利き手の手のひら側へ移る。
    /// 手元では向きも手に固定し、読める面を手のひらの向こうへ向ける（手首を返して手のひらを見ると正対する）。
    /// 手の姿勢が使えない（非VR）間は視点の正面に留まり続ける。
    ///
    /// 目標位置はフェーズごとに毎フレーム決め直し、実際の姿勢はそこへ指数補間で追いつかせる。
    /// フェーズ切り替えの瞬間に跳ばず、手ぶれもそのまま伝わらないようにするため。
    /// ただし追従先（頭または手）の「移動」ぶんは補間せず毎フレームそのまま足す。
    /// プレイヤーが歩いてカメラが動いたときはぴたりと付いてきて、首を回した（向きが変わった）ぶんだけが遅れて追いつく
    /// </summary>
    public class TutorialMessagePlacement
    {
        public TutorialMessagePhase Phase { get; private set; } = TutorialMessagePhase.Hidden;

        /// <summary>
        /// 直近の姿勢で、読める面が頭の方を向いているか（<see cref="TutorialMessagePlacementSettings.FacingAngle"/> 以内）。
        /// 手元では手首を返して手のひらを見たときだけ true になる
        /// </summary>
        public bool IsFacingHead { get; private set; }

        /// <summary>表示開始からの経過時間[s]</summary>
        private float _elapsed;

        /// <summary>表示後に一度でも配置したか。最初の1回は補間せず目標へスナップする</summary>
        private bool _isPlaced;

        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;

        /// <summary>前フレームの追従先の位置。移動ぶんを即時反映するための差分の基準</summary>
        private Vector3 _previousAnchorPosition;

        /// <summary>表示を開始し、視点正面フェーズからやり直す</summary>
        public void Begin()
        {
            Phase = TutorialMessagePhase.HeadFollow;
            IsFacingHead = false;
            _elapsed = 0f;
            _isPlaced = false;
        }

        public void End()
        {
            Phase = TutorialMessagePhase.Hidden;
            IsFacingHead = false;
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

            var previousPhase = Phase;
            Phase = anchor.IsHandAvailable && _elapsed >= settings.HeadFollowDuration
                ? TutorialMessagePhase.HandFollow
                : TutorialMessagePhase.HeadFollow;

            var isHandFollow = Phase == TutorialMessagePhase.HandFollow;
            var target = isHandFollow ? GetHandFollowPose(anchor, settings) : GetHeadFollowPose(anchor, settings);
            var anchorPosition = isHandFollow ? anchor.HandPose.position : anchor.HeadPose.position;

            if (!_isPlaced || settings.FollowSpeed <= 0f)
            {
                _position = target.position;
                _rotation = target.rotation;
                _isPlaced = true;
            }
            else
            {
                // 追従先の移動ぶんはそのまま足す（歩いたときにぴたりと付いてくる）。
                // フェーズが切り替わった直後は追従先が頭から手へ変わるので、その差分は移動として扱わない
                if (Phase == previousPhase)
                {
                    _position += anchorPosition - _previousAnchorPosition;
                }

                // 残り（向きの変化や切り替えによるずれ）はフレームレートに依存しないよう指数補間で追いつかせる
                var t = 1f - Mathf.Exp(-settings.FollowSpeed * deltaTime);
                _position = Vector3.Lerp(_position, target.position, t);
                _rotation = Quaternion.Slerp(_rotation, target.rotation, t);
            }

            _previousAnchorPosition = anchorPosition;
            // 向いている間は余白ぶん外れにくくし、境界付近の手ぶれで縮小・展開が繰り返されないようにする
            var facingAngle = IsFacingHead ? settings.FacingAngle + settings.FacingExitMargin : settings.FacingAngle;
            IsFacingHead = IsFacing(_position, _rotation, anchor.HeadPose.position, facingAngle);

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
        /// 非利き手の手のひら側。位置・向きとも手のローカル座標で決めて手に固定する。
        /// 設定は左手向けの値として扱い、右手のときは x を反転したオフセットと鏡写しの回転にする
        /// </summary>
        private static Pose GetHandFollowPose(
            in TutorialMessageAnchor anchor, in TutorialMessagePlacementSettings settings)
        {
            var offset = settings.HandOffset;
            var rotation = settings.HandRotation;
            if (anchor.Hand == HandType.Right)
            {
                offset.x = -offset.x;
                rotation = MirrorX(rotation);
            }

            var hand = anchor.HandPose;
            return new Pose(hand.position + hand.rotation * offset, hand.rotation * rotation);
        }

        /// <summary>YZ 平面で鏡写しにした回転（x 軸を反転した座標系での同じ回転）</summary>
        private static Quaternion MirrorX(Quaternion rotation)
        {
            return new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        /// <summary>
        /// 頭から見て読める面が向いているか。Canvas は前方が視線と同じ向きのときに正面から読めるため、
        /// 頭→メッセージの向きと前方とのなす角で判定する。頭と同じ位置にあって向きが定まらないときは向いていない扱い
        /// </summary>
        private static bool IsFacing(Vector3 position, Quaternion rotation, Vector3 headPosition, float maxAngle)
        {
            var direction = position - headPosition;
            if (direction.sqrMagnitude <= VectorConstants.DirectionEpsilon)
            {
                return false;
            }

            return Vector3.Angle(rotation * Vector3.forward, direction) <= maxAngle;
        }
    }
}
