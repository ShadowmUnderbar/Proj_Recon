using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data;
using App.Common.Data.MasterData;
using App.Common.Views;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// アップグレード候補を3Dカードとして並べ、掴んで内容を確認し、トリガーで確定させるVR用のボード。
    ///
    /// カードはショップを開いた時点のカメラ（HMD）前方へ一度だけ配置し、以降はワールド固定にする。
    /// 追従させると掴む対象が動いてしまい狙いにくくなるため、あえて置きっぱなしにしている。
    ///
    /// 掴みは「手を重ねて掴む（近接）」と「レイで狙って掴む（遠隔）」の両方に対応する。
    /// 近接のほうが意図が明確なため、近接で掴めるカードがあればそちらを優先する。
    /// </summary>
    public class UpgradeCardBoardView : MonoBehaviour
    {
        [SerializeField, Tooltip("カード1枚のプレハブ")]
        private UpgradeCardView _cardPrefab;

        [Header("配置")]
        [SerializeField, Tooltip("カメラからカードまでの距離[m]")]
        private float _distance = 0.9f;

        [SerializeField, Range(-180f, 180f), Tooltip("カードを配置する俯角[deg]。0で視線の正面、正の値で下側")]
        private float _pitchAngle = 30f;

        [SerializeField, Tooltip("1行あたりのカード枚数")]
        private int _columnCount = 5;

        [SerializeField, Tooltip("カードの間隔[m]（X:横 Y:縦）")]
        private Vector2 _cardSpacing = new(0.18f, 0.26f);

        [SerializeField, Tooltip("並べたカードの向き[deg]。カードの表がプレイヤー側を向くように調整する")]
        private Vector3 _cardFacingRotation = new(0f, 180f, 0f);

        [Header("持ったときの見え方")]
        [SerializeField, Tooltip("持っているときのカードの拡大率")]
        private float _holdScale = 1.2f;

        [SerializeField, Range(1f, 5f),
         Tooltip("手首のひねりの増幅率。1で手首どおり、大きいほど少ないひねりでカードが回る（裏面の確認用）")]
        private float _wristRollMultiplier = 2f;

        [SerializeField, Tooltip("カードが手や定位置へ追いつく速さ。大きいほど速い")]
        private float _followSpeed = 18f;

        [Header("掴み判定")]
        [SerializeField, Tooltip("手を重ねて掴める距離[m]")]
        private float _directGrabDistance = 0.12f;

        [SerializeField, Tooltip("レイで掴める距離[m]")]
        private float _rayGrabDistance = 5f;

        [SerializeField, Tooltip("カードのコライダーが属するレイヤー")]
        private LayerMask _cardLayerMask = ~0;

        /// <summary>方向ベクトルが実質ゼロかを判定するしきい値</summary>
        private const float DirectionEpsilon = 1e-6f;

        private readonly Subject<int> _onCardConfirmed = new();

        /// <summary>掴んだカードをトリガーで確定したときに、その候補インデックスを流す</summary>
        public Observable<int> OnCardConfirmed => _onCardConfirmed;

        private readonly List<UpgradeCardView> _cards = new();

        /// <summary>左右それぞれの掴み状態。インデックスは <see cref="HandType"/> と対応させる</summary>
        private readonly HandState[] _handStates =
        {
            new(HandType.Left),
            new(HandType.Right)
        };

        private Camera _targetCamera;

        /// <summary>
        /// レイ判定の結果を受けるバッファ。毎フレームのアロケーションを避けるため使い回す。
        /// RaycastNonAllocは距離順に詰めてくれないため、カード枚数ぶんは余裕を持たせる
        /// </summary>
        private readonly RaycastHit[] _rayHits = new RaycastHit[16];

        /// <summary>
        /// 候補ぶんのカードを生成し、カメラ前方に並べる。
        /// プレハブ未設定やカメラ未取得で並べられなかった場合は false を返し、呼び出し側でUIを出し分けられるようにする
        /// </summary>
        public bool Open(IReadOnlyList<UpgradeMasterData> upgrades)
        {
            Close();

            if (_cardPrefab == null || upgrades == null || upgrades.Count == 0)
            {
                return false;
            }

            if (!TryPlaceBoard())
            {
                return false;
            }

            var facingRotation = Quaternion.Euler(_cardFacingRotation);

            for (var i = 0; i < upgrades.Count; i++)
            {
                var card = Instantiate(_cardPrefab, transform);
                var localPosition = GetCardLocalPosition(i, upgrades.Count);
                var homePose = new Pose(
                    transform.TransformPoint(localPosition),
                    transform.rotation * facingRotation);

                card.Setup(i, upgrades[i], homePose);
                _cards.Add(card);
            }

            return true;
        }

        /// <summary>指定インデックスのカードだけを取り除く（選択済みのカードを消す用）</summary>
        public void RemoveCard(int index)
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Index != index)
                {
                    continue;
                }

                ReleaseCardFromHands(_cards[i]);
                Destroy(_cards[i].gameObject);
                _cards.RemoveAt(i);
                return;
            }
        }

        /// <summary>すべてのカードを破棄し、掴み状態を初期化する</summary>
        public void Close()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();

            foreach (var handState in _handStates)
            {
                handState.Reset();
            }
        }

        /// <summary>片手ぶんの入力を受け取る。判定・移動は LateUpdate でまとめて行う</summary>
        public void UpdateHandInput(in ShopHandInput input)
        {
            _handStates[(int)input.HandType].SetInput(input);
        }

        private void LateUpdate()
        {
            if (_cards.Count == 0)
            {
                return;
            }

            foreach (var handState in _handStates)
            {
                UpdateHand(handState);
            }

            // 掴まれていないカードは定位置へ戻す
            foreach (var card in _cards)
            {
                if (IsHeld(card))
                {
                    continue;
                }

                card.MoveToHome(_followSpeed);
            }
        }

        /// <summary>片手ぶんの掴み・追従・確定を処理する</summary>
        private void UpdateHand(HandState handState)
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
                var hovered = FindGrabTarget(pointingPose);

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
                // 手を離したら定位置へ戻す
                handState.HeldCard.SetHighlight(CardHighlight.None);
                handState.ReleaseHeld();
            }
            else
            {
                var holdPose = handState.HeldCard.CalcHoldPose(
                    ApplyWristRoll(pointingPose, handState), input.HandType);

                handState.HeldCard.MoveTo(holdPose, Vector3.one * _holdScale, _followSpeed);

                // 持っている状態でトリガーを引いた瞬間に確定する
                if (input.IsConfirming && !handState.PreviousConfirming)
                {
                    _onCardConfirmed.OnNext(handState.HeldCard.Index);
                }
            }

            handState.StoreFrameState(pointingPose.rotation);
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
        private Pose ApplyWristRoll(Pose pointingPose, HandState handState)
        {
            // 直前フレームからの回転のうち、手の前方軸まわりのひねりだけを取り出す
            var frameRotation = Quaternion.Inverse(handState.PreviousPointingRotation) * pointingPose.rotation;
            handState.AccumulateRoll(GetTwistAngle(frameRotation, Vector3.forward));

            if (_wristRollMultiplier <= 1f)
            {
                return pointingPose;
            }

            var extraAngle = handState.AccumulatedRoll * (_wristRollMultiplier - 1f);

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
            if (projected.sqrMagnitude + rotation.w * rotation.w <= DirectionEpsilon)
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

        /// <summary>
        /// 掴む対象のカード。手を重ねている（近接）カードを優先し、無ければレイの先のカードを返す。
        /// すでにもう一方の手が持っているカードは対象にしない
        /// </summary>
        private UpgradeCardView FindGrabTarget(Pose handPose)
        {
            var nearest = FindNearestCard(handPose.position);

            return nearest != null ? nearest : FindCardByRay(handPose);
        }

        private UpgradeCardView FindNearestCard(Vector3 handPosition)
        {
            UpgradeCardView nearest = null;
            var nearestDistance = _directGrabDistance;

            foreach (var card in _cards)
            {
                if (IsHeld(card))
                {
                    continue;
                }

                // コライダーが未設定でも掴めるよう、その場合はカード中心までの距離で判定する
                var closestPoint = card.Collider != null
                    ? card.Collider.ClosestPoint(handPosition)
                    : card.transform.position;
                var distance = Vector3.Distance(handPosition, closestPoint);

                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = card;
                nearestDistance = distance;
            }

            return nearest;
        }

        private UpgradeCardView FindCardByRay(Pose handPose)
        {
            var hitCount = Physics.RaycastNonAlloc(
                handPose.position,
                handPose.rotation * Vector3.forward,
                _rayHits,
                _rayGrabDistance,
                _cardLayerMask,
                QueryTriggerInteraction.Collide);

            UpgradeCardView nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < hitCount; i++)
            {
                var card = _rayHits[i].collider.GetComponentInParent<UpgradeCardView>();

                // 他方の手が持っているカードや、カード以外のコライダーは無視する
                if (card == null || IsHeld(card) || _rayHits[i].distance >= nearestDistance)
                {
                    continue;
                }

                nearest = card;
                nearestDistance = _rayHits[i].distance;
            }

            return nearest;
        }

        /// <summary>
        /// レイで狙っているカードの強調表示を更新する。
        /// もう一方の手が持っているカードの色は消さないよう、解除時に掴み状態を見てから戻す
        /// </summary>
        private void SetHovered(HandState handState, UpgradeCardView card)
        {
            var previous = handState.SetHovered(card);

            if (previous != null && previous != card && !IsHeld(previous))
            {
                previous.SetHighlight(CardHighlight.None);
            }

            if (card != null && previous != card)
            {
                card.SetHighlight(CardHighlight.Hovered);
            }
        }

        private bool IsHeld(UpgradeCardView card)
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

        private void ReleaseCardFromHands(UpgradeCardView card)
        {
            foreach (var handState in _handStates)
            {
                handState.Release(card);
            }
        }

        /// <summary>
        /// ボード自体をカメラ前方の定位置へ置く。俯角ぶん傾けた向きへ配置し、その向きに正対させる
        /// （<see cref="VrUiFollowCanvasView"/> と同じ考え方だが、こちらは開いた瞬間に固定する）
        /// </summary>
        private bool TryPlaceBoard()
        {
            if (!TryGetTargetCamera(out var targetCamera))
            {
                return false;
            }

            var cameraTransform = targetCamera.transform;
            var yawForward = Quaternion.AngleAxis(-_pitchAngle, cameraTransform.right) * cameraTransform.forward;
            yawForward.y = 0f;

            // 想定した俯角から大きく外れて水平成分が消えたときは、頭の上方向を代わりの基準にする
            if (yawForward.sqrMagnitude <= DirectionEpsilon)
            {
                yawForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            }

            if (yawForward.sqrMagnitude <= DirectionEpsilon)
            {
                yawForward = Vector3.forward;
            }

            yawForward.Normalize();

            var right = Vector3.Cross(Vector3.up, yawForward);
            var pitchRotation = Quaternion.AngleAxis(_pitchAngle, right.normalized);
            var direction = pitchRotation * yawForward;
            var up = pitchRotation * Vector3.up;

            transform.SetPositionAndRotation(
                cameraTransform.position + direction * _distance,
                Quaternion.LookRotation(direction, up));

            return true;
        }

        /// <summary>ボード内でのカードの位置。左右・上下の中央揃えで格子状に並べる</summary>
        private Vector3 GetCardLocalPosition(int index, int totalCount)
        {
            var columnCount = Mathf.Max(1, _columnCount);
            var rowCount = Mathf.CeilToInt((float)totalCount / columnCount);
            var row = index / columnCount;
            var column = index % columnCount;

            // 最終行は枚数が欠けることがあるため、その行の枚数で中央揃えする
            var countInRow = Mathf.Min(columnCount, totalCount - row * columnCount);

            var x = (column - (countInRow - 1) * 0.5f) * _cardSpacing.x;
            var y = -(row - (rowCount - 1) * 0.5f) * _cardSpacing.y;

            return new Vector3(x, y, 0f);
        }

        private bool TryGetTargetCamera(out Camera targetCamera)
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            targetCamera = _targetCamera;
            return targetCamera != null;
        }

        private void OnDestroy()
        {
            _onCardConfirmed.Dispose();
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

            /// <summary>直前フレームの手の向き。ひねりの差分を測る基準にする</summary>
            public Quaternion PreviousPointingRotation { get; private set; } = Quaternion.identity;

            /// <summary>掴んでからの累計のひねり角[deg]</summary>
            public float AccumulatedRoll { get; private set; }

            private UpgradeCardView _hoveredCard;

            public void SetInput(in ShopHandInput input)
            {
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

            /// <summary>次フレームでの差分・押下エッジ判定のために、このフレームの状態を控える</summary>
            public void StoreFrameState(Quaternion pointingRotation)
            {
                PreviousGrabbing = Input.IsGrabbing;
                PreviousConfirming = Input.IsConfirming;
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
