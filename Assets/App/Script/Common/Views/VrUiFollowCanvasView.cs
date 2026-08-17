using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// VR向けWorldSpace UIのCanvasを、カメラ（VRではHMD）の正面へ遅延追従させる。
    /// 表示された瞬間は正面へスナップし、以降は視点が少し動いた程度では位置を固定したままにする。
    /// しきい値を超えて視点が動いた（または大きく移動した）ときだけ、正面へ向かってゆっくり追従する。
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
        [SerializeField, Tooltip("カメラからUIまでの水平距離[m]")]
        private float _distance = 1.5f;

        [SerializeField, Tooltip("カメラ基準の高さオフセット[m]。負値で目線より下に配置する")]
        private float _verticalOffset = -0.1f;

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

        [SerializeField, Tooltip("追従の速さ。大きいほど速く正面へ戻る")]
        private float _followSpeed = 3f;

        [SerializeField, Tooltip("非VR時のカメラ相対位置[m]。見下ろしカメラでも画面内に収まるようにする")]
        private Vector3 _nonVrLocalPosition = new(0f, 0f, 1.2f);

        private Canvas _canvas;
        private Camera _targetCamera;

        /// <summary>正面へ向けて移動中かどうか。しきい値にヒステリシスを持たせて小刻みな追従を防ぐ</summary>
        private bool _isFollowing;

        /// <summary>初回の配置（スナップ）が済んでいるか</summary>
        private bool _isPlaced;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            // 再表示のたびに正面へ置き直す
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
            var targetPosition = cameraTransform.position + forward * _distance + Vector3.up * _verticalOffset;

            if (!_isPlaced)
            {
                ApplyPose(targetPosition, cameraTransform.position);
                _isPlaced = true;
                return;
            }

            UpdateFollowState(cameraTransform, forward);

            if (!_isFollowing)
            {
                return;
            }

            // 遅れて追いつく動き。フレームレートに依存しないよう指数補間を使う
            var t = 1f - Mathf.Exp(-_followSpeed * Time.unscaledDeltaTime);
            ApplyPose(Vector3.Lerp(transform.position, targetPosition, t), cameraTransform.position);
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
        private void UpdateFollowState(Transform cameraTransform, Vector3 forward)
        {
            var toUi = transform.position - cameraTransform.position;
            toUi.y = 0f;

            // 真上・真下を向いた場合など水平成分が消えたときは判定を据え置く
            if (toUi.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var angle = Vector3.Angle(forward, toUi.normalized);
            var horizontalDistance = toUi.magnitude;
            var distanceGap = Mathf.Abs(horizontalDistance - _distance);
            var heightGap = Mathf.Abs(transform.position.y - (cameraTransform.position.y + _verticalOffset));
            var positionGap = Mathf.Max(distanceGap, heightGap);

            if (_isFollowing)
            {
                _isFollowing = angle > _followStopAngle || positionGap > _followStopDistance;
                return;
            }

            _isFollowing = angle > _followStartAngle || positionGap > _followStartDistance;
        }

        /// <summary>指定位置へ移動し、カメラの方を向く（Canvasの表面がカメラ側を向く）</summary>
        private void ApplyPose(Vector3 position, Vector3 cameraPosition)
        {
            var lookDirection = position - cameraPosition;
            lookDirection.y = 0f;

            var rotation = lookDirection.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : transform.rotation;

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * _localScale;
        }

        /// <summary>カメラの前方から水平成分だけを取り出す。真上・真下を向いている場合はUIの向きを維持する</summary>
        private Vector3 GetHorizontalForward(Transform cameraTransform)
        {
            var forward = cameraTransform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > Mathf.Epsilon)
            {
                return forward.normalized;
            }

            // 見上げ・見下ろしで前方が失われたときは、頭の傾き（up）を代用して正面を決める
            var fallback = -cameraTransform.up * Mathf.Sign(cameraTransform.forward.y);
            fallback.y = 0f;

            return fallback.sqrMagnitude > Mathf.Epsilon ? fallback.normalized : transform.forward;
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
