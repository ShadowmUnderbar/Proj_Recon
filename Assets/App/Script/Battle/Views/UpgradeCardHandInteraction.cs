using System;
using App.Battle.Data;
using App.Common.Data;
using App.Common.Views;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// VRの両手でカードを掴む・持って眺める・トリガーで確定する、の状態機械。
    /// 左右の手ごとに「狙っているカード」「持っているカード」「手首のひねりの累計」を持ち、
    /// 毎フレームの入力からカードの強調表示・追従先・確定を決める。
    /// カードの検索は <see cref="UpgradeCardFinder"/> に任せ、ここは掴みの状態遷移だけを扱う
    /// </summary>
    public class UpgradeCardHandInteraction
    {
        /// <summary>左右それぞれの掴み状態。インデックスは <see cref="HandType"/> と対応させる</summary>
        private readonly HandState[] _handStates =
        {
            new(HandType.Left),
            new(HandType.Right)
        };

        /// <summary>片手ぶんの入力を控える。判定は <see cref="Update"/> でまとめて行う</summary>
        public void SetInput(in ShopHandInput input)
        {
            _handStates[(int)input.HandType].SetInput(input);
        }

        /// <summary>どちらかの手が持っているカードか</summary>
        public bool IsHeld(UpgradeCardView card)
        {
            foreach (var handState in _handStates)
            {
                if (handState.HeldCard == card)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>破棄されるカードを両手の掴み・ホバー対象から外す</summary>
        public void Release(UpgradeCardView card)
        {
            foreach (var handState in _handStates)
            {
                handState.Release(card);
            }
        }

        public void Reset()
        {
            foreach (var handState in _handStates)
            {
                handState.Reset();
            }
        }

        /// <summary>両手ぶんの掴み・追従・確定を処理する</summary>
        /// <param name="finder">カードの検索</param>
        /// <param name="grabSettings">カードを探す判定範囲</param>
        /// <param name="holdSettings">持ったときの見え方</param>
        /// <param name="onCardConfirmed">持っているカードをトリガーで確定したときに、その候補インデックスで呼ばれる</param>
        public void Update(
            UpgradeCardFinder finder,
            in UpgradeCardGrabSettings grabSettings,
            in UpgradeCardHoldSettings holdSettings,
            Action<int> onCardConfirmed)
        {
            foreach (var handState in _handStates)
            {
                UpdateHand(handState, finder, grabSettings, holdSettings, onCardConfirmed);
            }
        }

        private void UpdateHand(
            HandState handState,
            UpgradeCardFinder finder,
            in UpgradeCardGrabSettings grabSettings,
            in UpgradeCardHoldSettings holdSettings,
            Action<int> onCardConfirmed)
        {
            if (!handState.HasInput)
            {
                return;
            }

            var input = handState.Input;

            // コントローラのTransformそのままでは前方が上を向いてしまうため、指し示す向きへ直して扱う
            var pointingPose = GetPointingPose(input.Pose);

            if (handState.HeldCard == null)
            {
                var hovered = finder.FindGrabTarget(pointingPose, grabSettings);

                // 握り始めたフレームだけ掴む（握りっぱなしで別のカードへ乗り換えないようにする）
                if (hovered != null && input.IsGrabbing && !handState.PreviousGrabbing)
                {
                    SetHovered(handState, null);
                    handState.Hold(hovered, pointingPose.rotation);
                    hovered.SetHighlight(CardHighlight.Held);
                }
                else
                {
                    SetHovered(handState, hovered);
                }
            }
            else if (!input.IsGrabbing)
            {
                // 手を離したら定位置へ戻す（戻す移動そのものはボード側が行う）
                handState.HeldCard.SetHighlight(CardHighlight.None);
                handState.ReleaseHeld();
            }
            else
            {
                var holdPose = handState.HeldCard.CalcHoldPose(
                    ApplyWristRoll(pointingPose, handState, holdSettings.WristRollMultiplier), input.HandType);

                handState.HeldCard.MoveTo(holdPose, Vector3.one * holdSettings.HoldScale, holdSettings.FollowSpeed);

                // 持っている状態でトリガーを引いた瞬間に確定する。買えないカードは読めるだけで確定させない
                if (input.IsConfirming && !handState.PreviousConfirming && handState.HeldCard.IsPurchasable)
                {
                    onCardConfirmed(handState.HeldCard.Index);
                }
            }

            handState.StorePointingRotation(pointingPose.rotation);
        }

        /// <summary>
        /// レイで狙っているカードの強調表示を更新する。
        /// もう一方の手が持っている・狙っているカードの色は消さないよう、解除時に相手の状態を見てから戻す
        /// </summary>
        private void SetHovered(HandState handState, UpgradeCardView card)
        {
            var previous = handState.SetHovered(card);

            if (previous != null && previous != card && !IsHeld(previous) && !IsHoveredByOther(handState, previous))
            {
                previous.SetHighlight(CardHighlight.None);
            }

            if (card != null && previous != card)
            {
                card.SetHighlight(CardHighlight.Hovered);
            }
        }

        /// <summary>指定した手以外の手が、そのカードを狙っているか</summary>
        private bool IsHoveredByOther(HandState self, UpgradeCardView card)
        {
            foreach (var handState in _handStates)
            {
                if (handState != self && handState.HoveredCard == card)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>コントローラの姿勢を、実際に指し示している向き（ハンドレイと同じ基準）へ直す</summary>
        private static Pose GetPointingPose(Pose handPose)
        {
            return new Pose(handPose.position, handPose.rotation * PlatformHandRotation.PointingAdjustment);
        }

        /// <summary>
        /// 手首のひねりを増幅した手の姿勢。実際の手首は180度も回らないため、
        /// そのままでは裏面を覗き込みにくいことへの対処として、ひねりぶんを上乗せする。
        ///
        /// ひねり量は毎フレームの差分を積み上げて求める。掴んだ瞬間からの絶対角で測ると
        /// ±180度をまたいだところで角度が一周ぶん飛び、カードが跳ねてしまう
        /// </summary>
        private static Pose ApplyWristRoll(Pose pointingPose, HandState handState, float wristRollMultiplier)
        {
            // 直前フレームからの回転のうち、手の前方軸まわりのひねりだけを取り出す
            var frameRotation = Quaternion.Inverse(handState.PreviousPointingRotation) * pointingPose.rotation;
            handState.AccumulateRoll(GetTwistAngle(frameRotation, Vector3.forward));

            if (wristRollMultiplier <= 1f)
            {
                return pointingPose;
            }

            var extraAngle = handState.AccumulatedRoll * (wristRollMultiplier - 1f);

            // 測るときと同じ「手の前方」を軸に、増幅ぶんだけ余分に回す
            return new Pose(
                pointingPose.position,
                pointingPose.rotation * Quaternion.AngleAxis(extraAngle, Vector3.forward));
        }

        /// <summary>
        /// 回転のうち、指定軸まわりのひねり成分だけを角度[deg]で取り出す（スイング・ツイスト分解）。
        /// 単純に上方向を投影する方法では、軸と直交する向きへ大きく倒したときにひねりと誤認してしまう
        /// </summary>
        private static float GetTwistAngle(Quaternion rotation, Vector3 axis)
        {
            var rotationAxis = new Vector3(rotation.x, rotation.y, rotation.z);
            var projected = Vector3.Project(rotationAxis, axis);
            var twist = new Quaternion(projected.x, projected.y, projected.z, rotation.w);

            // ひねり成分が定まらない（軸と直交する向きへ180度回っている）場合は回さない
            if (projected.sqrMagnitude + rotation.w * rotation.w <= VectorConstants.DirectionEpsilon)
            {
                return 0f;
            }

            twist.Normalize();
            twist.ToAngleAxis(out var angle, out var twistAxis);

            if (angle > 180f)
            {
                angle -= 360f;
            }

            // ToAngleAxisは軸の向きを正規化して返すため、指定軸と逆向きなら符号を反転する
            return Vector3.Dot(twistAxis, axis) < 0f ? -angle : angle;
        }

        /// <summary>片手ぶんの掴み状態。ボタンの押下エッジを取るために前フレームの状態も保持する</summary>
        private class HandState
        {
            public HandState(HandType handType)
            {
                Input = new ShopHandInput(handType, Pose.identity, false, false);
            }

            public ShopHandInput Input { get; private set; }
            public bool HasInput { get; private set; }
            public bool PreviousGrabbing { get; private set; }
            public bool PreviousConfirming { get; private set; }
            public UpgradeCardView HeldCard { get; private set; }

            /// <summary>狙っているカード。もう一方の手が強調表示を消してよいかの判断に使う</summary>
            public UpgradeCardView HoveredCard => _hoveredCard;

            /// <summary>直前フレームの手の向き。ひねりの差分を測る基準にする</summary>
            public Quaternion PreviousPointingRotation { get; private set; } = Quaternion.identity;

            /// <summary>掴んでからの累計のひねり角[deg]</summary>
            public float AccumulatedRoll { get; private set; }

            private UpgradeCardView _hoveredCard;

            /// <summary>
            /// 入力を差し替える。押下エッジの基準になる前フレームの状態もここで進める。
            /// 最初の1回だけは今の状態をそのまま前フレーム扱いにし、
            /// グラブ・トリガーを握ったままショップが開いても即座に掴んで確定しないようにする
            /// </summary>
            public void SetInput(in ShopHandInput input)
            {
                PreviousGrabbing = HasInput ? Input.IsGrabbing : input.IsGrabbing;
                PreviousConfirming = HasInput ? Input.IsConfirming : input.IsConfirming;
                Input = input;
                HasInput = true;
            }

            /// <summary>狙っているカードを差し替え、直前まで狙っていたカードを返す（強調表示の更新は呼び出し側）</summary>
            public UpgradeCardView SetHovered(UpgradeCardView card)
            {
                var previous = _hoveredCard;
                _hoveredCard = card;

                return previous;
            }

            /// <summary>カードを掴む。ひねりは掴んだ時点を0として測り直す</summary>
            public void Hold(UpgradeCardView card, Quaternion pointingRotation)
            {
                HeldCard = card;
                PreviousPointingRotation = pointingRotation;
                AccumulatedRoll = 0f;
            }

            public void AccumulateRoll(float angle)
            {
                AccumulatedRoll += angle;
            }

            /// <summary>持っているカードを離す</summary>
            public void ReleaseHeld()
            {
                HeldCard = null;
            }

            /// <summary>次フレームでのひねりの差分を測るために、このフレームの手の向きを控える</summary>
            public void StorePointingRotation(Quaternion pointingRotation)
            {
                PreviousPointingRotation = pointingRotation;
            }

            /// <summary>破棄されるカードを掴み・ホバー対象から外す</summary>
            public void Release(UpgradeCardView card)
            {
                if (HeldCard == card)
                {
                    HeldCard = null;
                }

                if (_hoveredCard == card)
                {
                    _hoveredCard = null;
                }
            }

            public void Reset()
            {
                HeldCard = null;
                PreviousPointingRotation = Quaternion.identity;
                AccumulatedRoll = 0f;
                _hoveredCard = null;
                HasInput = false;
                PreviousGrabbing = false;
                PreviousConfirming = false;
            }
        }
    }
}
