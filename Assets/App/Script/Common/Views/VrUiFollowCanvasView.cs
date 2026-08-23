using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// VR向けWorldSpace UIのCanvasを、カメラ（VRではHMD）の向きへ遅延追従させる。
    /// 配置する向きは<see cref="_pitchAngle"/>で指定でき、0なら正面、90なら真下に置ける。
    /// 本作は基本的にカメラが下向きのため既定では下側へ配置し、UIによっては正面寄りに調整できるようにしている。
    ///
    /// 追従の基準は「首が横を向いたか（ヨー）」と「頭が移動したか」の2つだけで判定する。
    /// 首の上下（ピッチ）は追従の対象にしない。俯角90付近ではUIがほぼ真下に来るため、
    /// カメラ前方の水平成分をそのままヨー基準にすると、真下付近で水平成分が消えて向きが反転してしまう。
    /// そこでカメラ前方を俯角ぶん上へ戻した方向をヨー基準にし、指定した俯角で頭を向けているときに
    /// もっとも安定するようにしている（俯角0なら従来どおりカメラ前方の水平成分と一致する）。
    ///
    /// 表示された瞬間は定位置へスナップし、以降は視点が少し動いた程度では位置を固定したままにする。
    /// しきい値を超えて首が回った（または大きく移動した）ときだけ、定位置へ向かってゆっくり追従する。
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

        [SerializeField, Range(-180f, 180f), Tooltip("UIを配置する俯角[deg]。0で視線の正面、正の値で下側（90で真下）、負の値で上側に配置する")]
        private float _pitchAngle = 30f;

        [SerializeField, Tooltip("Canvasのスケール。1px＝何mかを表す")]
        private float _localScale = 0.001f;

        [SerializeField, Tooltip("首の向き（ヨー）がこの角度[deg]を超えてUIからずれたら追従を開始する")]
        private float _followStartAngle = 25f;

        [SerializeField, Tooltip("追従を終了する角度[deg]。_followStartAngleより小さくすること")]
        private float _followStopAngle = 3f;

        [SerializeField, Tooltip("UIを置いた時点の頭の位置からこの距離[m]を超えて移動したら追従を開始する")]
        private float _followStartDistance = 0.5f;

        [SerializeField, Tooltip("追従を終了する距離[m]。_followStartDistanceより小さくすること")]
        private float _followStopDistance = 0.05f;

        [SerializeField, Tooltip("追従の速さ。大きいほど速く定位置へ戻る")]
        private float _followSpeed = 3f;

        [SerializeField, Tooltip("頭がこの距離[m]以上動いたら補間せず即座に定位置へ置き直す（トラッキング開始時の飛び対策）")]
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

        /// <summary>UIを配置しているヨー方向（水平・正規化済み）。カメラのヨーに遅れて追いつく</summary>
        private Vector3 _placedYawForward = Vector3.forward;

        /// <summary>UIを配置した基準となる頭の位置。カメラの移動に遅れて追いつく</summary>
        private Vector3 _placedHeadPosition;

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

            var headPosition = cameraTransform.position;
            var yawReference = GetYawReference(cameraTransform);
            var headMovement = Vector3.Distance(_placedHeadPosition, headPosition);

            // 初回配置に加え、XRトラッキング開始時のようにカメラが大きく飛んだ場合も置き直す。
            // 補間で追わせると、UIが視界を横切って滑っていく見え方になってしまう
            if (!_isPlaced || headMovement > _snapDistance)
            {
                _placedYawForward = yawReference;
                _placedHeadPosition = headPosition;
                _isPlaced = true;
                _isFollowing = false;
                ApplyPose();
                return;
            }

            UpdateFollowState(Vector3.Angle(_placedYawForward, yawReference), headMovement);

            if (_isFollowing)
            {
                // 遅れて追いつく動き。フレームレートに依存しないよう指数補間を使う。
                // ヨーと頭の位置だけを補間するので、カメラとUIの距離は常に_distanceに保たれる
                var t = 1f - Mathf.Exp(-_followSpeed * Time.unscaledDeltaTime);
                _placedYawForward = SlerpYaw(_placedYawForward, yawReference, t);
                _placedHeadPosition = Vector3.Lerp(_placedHeadPosition, headPosition, t);
            }

            ApplyPose();
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
        private void UpdateFollowState(float yawGap, float headMovement)
        {
            if (_isFollowing)
            {
                _isFollowing = yawGap > _followStopAngle || headMovement > _followStopDistance;
                return;
            }

            _isFollowing = yawGap > _followStartAngle || headMovement > _followStartDistance;
        }

        /// <summary>
        /// ヨー方向の球面補間。真後ろ（180度）を向いたときは補間軸が定まらず結果が壊れるため、
        /// 水平成分だけを取り出して正規化し直し、それでも定まらない場合は目標のヨーへ直接合わせる
        /// </summary>
        private static Vector3 SlerpYaw(Vector3 from, Vector3 to, float t)
        {
            var yaw = Vector3.Slerp(from, to, t);
            yaw.y = 0f;

            return yaw.sqrMagnitude > DirectionEpsilon ? yaw.normalized : to;
        }

        /// <summary>
        /// カメラのヨー基準となる水平方向。カメラ前方を俯角ぶん上へ戻してから水平成分を取り出すため、
        /// 俯角90（真下配置）でも真下を向いた姿勢でもっとも安定し、向きが反転しない
        /// </summary>
        private Vector3 GetYawReference(Transform cameraTransform)
        {
            var reference = Quaternion.AngleAxis(-_pitchAngle, cameraTransform.right) * cameraTransform.forward;
            reference.y = 0f;

            if (reference.sqrMagnitude > DirectionEpsilon)
            {
                return reference.normalized;
            }

            // 想定した俯角から90度近く外れて水平成分が消えたときは、直前のヨーを維持して反転を防ぐ
            return _placedYawForward;
        }

        /// <summary>
        /// 配置しているヨーと頭の位置からUIの姿勢を作る。
        /// 俯角ぶん傾けた分だけUIも傾き、指定した俯角で頭を向けたときに正対して読める
        /// </summary>
        private void ApplyPose()
        {
            var right = Vector3.Cross(Vector3.up, _placedYawForward);

            if (right.sqrMagnitude <= DirectionEpsilon)
            {
                return;
            }

            // 俯角ぶん配置方向とUIの上方向をまとめて倒す。上方向も一緒に倒すことで、
            // 真下配置でもUIの上端が首の向いている方向を指し、上下逆さまにならない
            var pitchRotation = Quaternion.AngleAxis(_pitchAngle, right.normalized);
            var direction = pitchRotation * _placedYawForward;
            var up = pitchRotation * Vector3.up;

            transform.SetPositionAndRotation(
                _placedHeadPosition + direction * _distance,
                Quaternion.LookRotation(direction, up));
            transform.localScale = Vector3.one * _localScale;
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
