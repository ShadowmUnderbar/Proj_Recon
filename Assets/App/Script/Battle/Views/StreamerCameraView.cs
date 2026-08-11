using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using R3;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace App.Battle.Views
{
    /// <summary>
    /// ディスプレイ（配信）側だけに映るカメラ。
    /// 通常はプレイヤーカメラの視点を複製し、演出中のみ被写体を収める位置・画角へ移動して元の視点へ戻る。
    /// HMD側の描画はプレイヤーカメラが担当したままなので、この処理はVR体験に影響しない。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class StreamerCameraView : MonoBehaviour, IStreamerCameraView
    {
        private enum ShotPhase
        {
            Idle,
            BlendIn,
            Hold,
            BlendOut,
        }

        private StreamerModeConfig _streamerModeConfig;
        private StreamerCameraFramingCalculator _framingCalculator;

        private readonly Subject<Unit> _onShotFinished = new();
        public Observable<Unit> OnShotFinished => _onShotFinished;

        private Camera _camera;
        private Camera _sourceCamera;
        private bool _isAvailable;

        // プレイヤー視点追従の現在値。演出中も裏で更新し続け、復帰時に最新のHMD姿勢へ戻れるようにする
        private Vector3 _followPosition;
        private Quaternion _followRotation = Quaternion.identity;
        private bool _hasFollowState;

        private StreamerCameraShotRequest _request;
        private ShotPhase _phase = ShotPhase.Idle;
        private float _shotElapsedTime;
        private Vector3 _shotPivot;
        private float _shotDistance;
        private float _shotBaseYaw;

        [Inject]
        public void Construct(
            StreamerModeConfig streamerModeConfig,
            StreamerCameraFramingCalculator framingCalculator
        )
        {
            _streamerModeConfig = streamerModeConfig;
            _framingCalculator = framingCalculator;

            Setup();
        }

        private void Setup()
        {
            _camera = GetComponent<Camera>();

            _isAvailable = _streamerModeConfig.IsEnabled
                           && (!_streamerModeConfig.VROnly || DebugConfig.IsVRMode);

            if (!_isAvailable)
            {
                // 無効時は描画コストを一切かけない
                _camera.enabled = false;
                gameObject.SetActive(false);
                return;
            }

            if (CompareTag("MainCamera"))
            {
                Debug.LogError("StreamerCameraViewにMainCameraタグが付いています。Camera.mainがプレイヤーカメラを指さなくなるため外してください");
            }

            _camera.targetDisplay = _streamerModeConfig.TargetDisplay;
            _camera.depth = _streamerModeConfig.CameraDepth;
            _camera.cullingMask = _streamerModeConfig.CullingMask;
            _camera.fieldOfView = _streamerModeConfig.FieldOfView;

            // 配信用カメラは単眼で描く。XRのステレオ描画対象から外す
            _camera.stereoTargetEye = StereoTargetEyeMask.None;

            var additionalCameraData = _camera.GetUniversalAdditionalCameraData();
            if (additionalCameraData != null)
            {
                additionalCameraData.allowXRRendering = false;
            }

            _camera.enabled = true;
        }

        public void PlayShot(StreamerCameraShotRequest request)
        {
            if (!_isAvailable || request?.ShotData == null)
            {
                return;
            }

            if (!TryResolveSourceCamera())
            {
                return;
            }

            _request = request;
            _shotElapsedTime = 0f;
            _phase = ShotPhase.BlendIn;

            SetupShotFraming(request);
        }

        public void CancelShot()
        {
            if (_phase == ShotPhase.Idle)
            {
                return;
            }

            FinishShot();
        }

        // 被写体から注視点・距離・基準角を確定させる。演出中に被写体が消えても構図が崩れないよう最初に固定する
        private void SetupShotFraming(StreamerCameraShotRequest request)
        {
            var shotData = request.ShotData;
            var sourcePosition = _sourceCamera.transform.position;

            if (!_framingCalculator.TryCalculateBounds(request.SubjectPositions, out var center, out var radius))
            {
                // 被写体が無いトリガー（ウェーブ進行など）はプレイヤーの前方を被写体として扱う
                center = sourcePosition + FlattenDirection(_sourceCamera.transform.forward) * shotData.Distance;
                radius = 0f;
            }

            _shotPivot = center;

            _shotDistance = shotData.UseAutoFraming
                ? _framingCalculator.CalculateFitDistance(
                    radius,
                    shotData.FieldOfView,
                    _camera.aspect,
                    shotData.FramingMargin,
                    shotData.Distance)
                : shotData.Distance;

            // 被写体からプレイヤーを見た向きを基準にすることで、プレイヤーを画面から追い出さない構図になる
            var pivotToPlayer = FlattenDirection(sourcePosition - _shotPivot);
            var baseYaw = Quaternion.LookRotation(pivotToPlayer, Vector3.up).eulerAngles.y;
            _shotBaseYaw = baseYaw + shotData.YawOffset;
        }

        private void LateUpdate()
        {
            if (!_isAvailable)
            {
                return;
            }

            if (!TryResolveSourceCamera())
            {
                return;
            }

            UpdateFollowState(Time.deltaTime);

            if (_phase == ShotPhase.Idle)
            {
                ApplyPose(_followPosition, _followRotation, _streamerModeConfig.FieldOfView);
                return;
            }

            UpdateShot(Time.deltaTime);
        }

        // プレイヤーカメラの姿勢を追従値へ反映する。TrackedPoseDriverが書き込んだ姿勢をそのまま複製する
        private void UpdateFollowState(float deltaTime)
        {
            var sourceTransform = _sourceCamera.transform;
            var targetPosition = sourceTransform.position;
            var targetRotation = sourceTransform.rotation;

            var smoothTime = _streamerModeConfig.FollowSmoothTime;
            if (!_hasFollowState || smoothTime <= 0f || deltaTime <= 0f)
            {
                _followPosition = targetPosition;
                _followRotation = targetRotation;
                _hasFollowState = true;
                return;
            }

            // フレームレートに依存しない指数補間
            var t = 1f - Mathf.Exp(-deltaTime / smoothTime);
            _followPosition = Vector3.Lerp(_followPosition, targetPosition, t);
            _followRotation = Quaternion.Slerp(_followRotation, targetRotation, t);
        }

        private void UpdateShot(float deltaTime)
        {
            var shotData = _request.ShotData;
            _shotElapsedTime += deltaTime;

            var blendWeight = CalculateBlendWeight(shotData, _shotElapsedTime, out var phase);
            _phase = phase;

            if (_phase == ShotPhase.Idle)
            {
                FinishShot();
                ApplyPose(_followPosition, _followRotation, _streamerModeConfig.FieldOfView);
                return;
            }

            CalculateShotPose(shotData, _shotElapsedTime, out var shotPosition, out var shotRotation);

            var position = Vector3.Lerp(_followPosition, shotPosition, blendWeight);
            var rotation = Quaternion.Slerp(_followRotation, shotRotation, blendWeight);
            var fieldOfView = Mathf.Lerp(_streamerModeConfig.FieldOfView, shotData.FieldOfView, blendWeight);

            ApplyPose(position, rotation, fieldOfView);
        }

        // 0=プレイヤー視点 / 1=演出画角 のブレンド率を求める
        private float CalculateBlendWeight(StreamerCameraShotData shotData, float elapsedTime, out ShotPhase phase)
        {
            var blendInDuration = shotData.BlendInDuration;
            var holdEndTime = blendInDuration + shotData.HoldDuration;
            var totalDuration = shotData.TotalDuration;

            if (elapsedTime >= totalDuration)
            {
                phase = ShotPhase.Idle;
                return 0f;
            }

            if (elapsedTime < blendInDuration)
            {
                phase = ShotPhase.BlendIn;
                return Mathf.SmoothStep(0f, 1f, elapsedTime / blendInDuration);
            }

            if (elapsedTime < holdEndTime)
            {
                phase = ShotPhase.Hold;
                return 1f;
            }

            phase = ShotPhase.BlendOut;

            var blendOutDuration = shotData.BlendOutDuration;
            if (blendOutDuration <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, (elapsedTime - holdEndTime) / blendOutDuration);
        }

        private void CalculateShotPose(
            StreamerCameraShotData shotData,
            float elapsedTime,
            out Vector3 position,
            out Quaternion rotation
        )
        {
            var yaw = _shotBaseYaw;
            if (shotData.ShotType == StreamerCameraShotType.Orbit)
            {
                yaw += shotData.OrbitSpeed * elapsedTime;
            }

            var direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var heightOffset = Vector3.up * shotData.Height;

            position = shotData.ShotType == StreamerCameraShotType.OverShoulder
                // プレイヤーの背後へ引いて肩越しに被写体を収める
                ? _sourceCamera.transform.position + direction * _shotDistance + heightOffset
                : _shotPivot + direction * _shotDistance + heightOffset;

            var lookAtPoint = _shotPivot + Vector3.up * shotData.LookAtHeight;
            var toLookAt = lookAtPoint - position;

            rotation = toLookAt.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(toLookAt, Vector3.up)
                : _followRotation;
        }

        private void ApplyPose(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            var cameraTransform = transform;
            cameraTransform.SetPositionAndRotation(position, rotation);
            _camera.fieldOfView = fieldOfView;
        }

        private void FinishShot()
        {
            _phase = ShotPhase.Idle;
            _request = null;
            _shotElapsedTime = 0f;
            _onShotFinished.OnNext(Unit.Default);
        }

        // プレイヤーカメラは実行時にプレハブから生成されるため、取得できるまで毎フレーム探す
        private bool TryResolveSourceCamera()
        {
            if (_sourceCamera != null)
            {
                return true;
            }

            _sourceCamera = Camera.main;
            return _sourceCamera != null;
        }

        // 水平成分だけを取り出して正規化する。真上・真下方向だけの入力にはフォールバックを返す
        private static Vector3 FlattenDirection(Vector3 direction)
        {
            var flattened = new Vector3(direction.x, 0f, direction.z);
            return flattened.sqrMagnitude > Mathf.Epsilon ? flattened.normalized : Vector3.forward;
        }

        private void OnDestroy()
        {
            _onShotFinished.Dispose();
        }
    }
}
