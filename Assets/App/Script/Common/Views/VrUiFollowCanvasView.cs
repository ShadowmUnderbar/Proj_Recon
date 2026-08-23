using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// VR向けWorldSpace UIのCanvasを、カメラ（VRではHMD）の視線方向へ遅延追従させる。
    /// 配置する向きは<see cref="_pitchAngle"/>で指定でき、0なら正面、正の値なら視線より下側に置ける。
    /// 本作は基本的にカメラが下向きのため既定では下側へ配置し、UIによっては正面寄りに調整できるようにしている。
    /// 表示された瞬間は定位置へスナップし、以降は視点が少し動いた程度では位置を固定したままにする。
    /// しきい値を超えて視点が動いた（または大きく移動した）ときだけ、定位置へ向かってゆっくり追従する。
    /// カメラに固定親子付けするとUIが顔に貼り付いて酔いやすいため、ワールド座標で追従させている。
    ///
    /// 非VR（PC）ではカメラが真上から見下ろす固定アングルのため、遅延追従は行わず
    /// 従来どおりカメラ相対の一定位置に貼り付ける。
    ///
    /// VRのハンドレイ（TrackedDeviceGraphicRaycaster）と非VRのマウス（GraphicRaycaster）の
    /// 両方でレイキャスト可能にするため、CanvasのRenderMode/worldCameraもここで設定する。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class VrUiFollowCanvasView : MonoBehaviour
    {
        [SerializeField, Tooltip("カメラからUIまでの距離[m]")]
        private float _distance = 1.5f;

        [SerializeField, Range(-80f, 80f), Tooltip("UIを配置する俯角[deg]。0で視線の正面、正の値で下側、負の値で上側に配置する")]
        private float _pitchAngle = 30f;

        [SerializeField, Tooltip("Canvasのスケール。1px＝何mかを表す")]
        private float _localScale = 0.001f;

        [SerializeField, Tooltip("視線がこの角度[deg]を超えてUIからずれたら追従を開始する")]
        private float _followStartAngle = 25f;

        [SerializeField, Tooltip("追従を終了する角度[deg]。_followStartAngleより小さくすること")]
        private float _followStopAngle = 3f;

        [SerializeField, Tooltip("UIの定位置からこの距離[m]を超えてカメラが移動したら追従を開始する")]
        private float _followStartDistance = 0.5f;

        [SerializeField, Tooltip("追従を終了する距離[m]。_followStartDistanceより小さくすること")]
        private float _followStopDistance = 0.05f;

        [SerializeField, Tooltip("追従の速さ。大きいほど速く定位置へ戻る")]
        private float _followSpeed = 3f;

        [SerializeField, Tooltip("この距離[m]以上ずれたら補間せず即座に定位置へ置き直す（トラッキング開始時の飛び対策）")]
        private float _snapDistance = 1f;

        [SerializeField, Tooltip("非VR時のカメラ相対位置[m]。見下ろしカメラでも画面内に収まるようにする")]
        private Vector3 _nonVrLocalPosition = new(0f, 0f, 1.2f);

        /// <summary>方向ベクトルが実質ゼロかを判定するしきい値。Mathf.Epsilonでは小さすぎて機能しない</summary>
        private const float DirectionEpsilon = 1e-6f;

        private Canvas _canvas;
        private Camera _targetCamera;

        /// <summary>定位置へ向けて移動中かどうか。しきい値にヒステリシスを持たせて小刻みな追従を防ぐ</summary>
        private bool _isFollowing;

        /// <summary>初回の配置（スナップ）が済んでいるか</summary>
        private bool _isPlaced;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            // 再表示のたびに定位置へ置き直す
            _isPlaced = false;
            _isFollowing = false;
        }

        private void LateUpdate()
        {
            if (!TryGetTargetCamera(out var targetCamera))
            {
                return;
            }

            var cameraTransform = targetCamera.transform;

            if (!DebugConfig.IsVRMode)
            {
                ApplyNonVrPose(cameraTransform);
                return;
            }

            var forward = GetHorizontalForward(cameraTransform);
            var targetPosition = cameraTransform.position + GetPlacementDirection(forward) * _distance;
            var positionGap = GetPositionGap(cameraTransform);

            // 初回配置に加え、XRトラッキング開始時のようにカメラが大きく飛んだ場合も置き直す。
            // 補間で追わせると、UIが視界を横切って滑っていく見え方になってしまう
            if (!_isPlaced || positionGap > _snapDistance)
            {
                ApplyPose(targetPosition, cameraTransform.position);
                _isPlaced = true;
                _isFollowing = false;
                return;
            }

            UpdateFollowState(cameraTransform, forward, positionGap);

            if (!_isFollowing)
            {
                return;
            }

            // 遅れて追いつく動き。フレームレートに依存しないよう指数補間を使う
            var t = 1f - Mathf.Exp(-_followSpeed * Time.unscaledDeltaTime);

            // 位置を直線で補間するとカメラとの距離が縮んで顔の近くを横切ってしまうため、
            // カメラからのオフセットを球面補間し、距離を保ったまま定位置へ回り込ませる
            var currentOffset = transform.position - cameraTransform.position;
            var targetOffset = targetPosition - cameraTransform.position;
            var offset = Vector3.Slerp(currentOffset, targetOffset, t);

            ApplyPose(cameraTransform.position + offset, cameraTransform.position);
        }

        /// <summary>
        /// 非VR時の配置。カメラ相対の固定位置に貼り付け、カメラの向き（見下ろし角を含む）に正対させる
        /// </summary>
        private void ApplyNonVrPose(Transform cameraTransform)
        {
            transform.SetPositionAndRotation(
                cameraTransform.TransformPoint(_nonVrLocalPosition),
                cameraTransform.rotation);
            transform.localScale = Vector3.one * _localScale;
        }

        /// <summary>
        /// 追従の開始・終了を判定する。開始と終了で別のしきい値を使い、境界での振動を防ぐ
        /// </summary>
        private void UpdateFollowState(Transform cameraTransform, Vector3 forward, float positionGap)
        {
            var toUi = transform.position - cameraTransform.position;
            toUi.y = 0f;

            // カメラの真上・真下にUIが来て水平成分が消えたときは、角度が求まらないので判定を据え置く
            if (toUi.sqrMagnitude <= DirectionEpsilon)
            {
                return;
            }

            var angle = Vector3.Angle(forward, toUi.normalized);

            if (_isFollowing)
            {
                _isFollowing = angle > _followStopAngle || positionGap > _followStopDistance;
                return;
            }

            _isFollowing = angle > _followStartAngle || positionGap > _followStartDistance;
        }

        /// <summary>
        /// 俯角ぶんだけ水平前方を倒した配置方向。0なら水平前方そのまま、正の値でカメラの下側を向く
        /// </summary>
        private Vector3 GetPlacementDirection(Vector3 horizontalForward)
        {
            var right = Vector3.Cross(Vector3.up, horizontalForward);

            return Quaternion.AngleAxis(_pitchAngle, right) * horizontalForward;
        }

        /// <summary>UIの現在位置と定位置（カメラ視線方向の既定距離・俯角）とのずれ[m]</summary>
        private float GetPositionGap(Transform cameraTransform)
        {
            var toUi = transform.position - cameraTransform.position;
            toUi.y = 0f;

            // 俯角ぶん傾けた配置では、定位置の水平距離と高さオフセットが距離の三角関数で決まる
            var pitchRadian = _pitchAngle * Mathf.Deg2Rad;
            var horizontalDistance = _distance * Mathf.Cos(pitchRadian);
            var verticalOffset = -_distance * Mathf.Sin(pitchRadian);

            var distanceGap = Mathf.Abs(toUi.magnitude - horizontalDistance);
            var heightGap = Mathf.Abs(transform.position.y - (cameraTransform.position.y + verticalOffset));

            return Mathf.Max(distanceGap, heightGap);
        }

        /// <summary>
        /// 指定位置へ移動し、カメラの方を向く（Canvasの表面がカメラ側を向く）。
        /// 俯角をつけた場合はUIも上向きに傾き、見下ろしたまま正対して読めるようにする
        /// </summary>
        private void ApplyPose(Vector3 position, Vector3 cameraPosition)
        {
            var lookDirection = position - cameraPosition;
            var rotation = GetLookRotation(lookDirection);

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * _localScale;
        }

        /// <summary>
        /// 指定方向を向く回転。UIを傾けたときにロールが不安定になるのを避けるため、
        /// ワールドの上方向ではなく配置方向から組み直した上方向を基準にする
        /// </summary>
        private Quaternion GetLookRotation(Vector3 lookDirection)
        {
            if (lookDirection.sqrMagnitude <= DirectionEpsilon)
            {
                return transform.rotation;
            }

            var forward = lookDirection.normalized;
            var right = Vector3.Cross(Vector3.up, forward);

            // 真上・真下を向くと右方向が求まらないので、その場合は現在の向きを維持する
            if (right.sqrMagnitude <= DirectionEpsilon)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(forward, Vector3.Cross(forward, right.normalized));
        }

        /// <summary>カメラの前方から水平成分だけを取り出す。真上・真下を向いている場合はUIの向きを維持する</summary>
        private Vector3 GetHorizontalForward(Transform cameraTransform)
        {
            var forward = cameraTransform.forward;
            forward.y = 0f;

            // ほぼ真上・真下を向くと水平成分が数値誤差レベルまで縮み、向きが不安定に反転するため
            // Mathf.Epsilonではなく実用的なしきい値で判定する
            if (forward.sqrMagnitude > DirectionEpsilon)
            {
                return forward.normalized;
            }

            // 見上げ・見下ろしで前方が失われたときは、頭の傾き（up）を代用して正面を決める
            var fallback = -cameraTransform.up * Mathf.Sign(cameraTransform.forward.y);
            fallback.y = 0f;

            return fallback.sqrMagnitude > DirectionEpsilon ? fallback.normalized : transform.forward;
        }

        /// <summary>描画・レイキャストの基準カメラ。破棄されている場合は取得し直す</summary>
        private bool TryGetTargetCamera(out Camera targetCamera)
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;

                if (_targetCamera != null)
                {
                    _canvas.renderMode = RenderMode.WorldSpace;
                    _canvas.worldCamera = _targetCamera;
                }
            }

            targetCamera = _targetCamera;
            return targetCamera != null;
        }
    }
}
