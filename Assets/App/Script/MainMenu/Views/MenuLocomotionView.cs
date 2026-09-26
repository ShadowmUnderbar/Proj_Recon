using App.Common.Data;
using App.MainMenu.Interface;
using UnityEngine;

namespace App.MainMenu.Views
{
    /// <summary>
    /// メインメニューの部屋を一人称で歩き回るための移動。
    /// このコンポーネントが載るオブジェクト（XRリグの親）をCharacterControllerで動かす。
    ///
    /// カプセルは毎フレームHMDの真下へ合わせている。リグ原点とHMDは実際に部屋を歩いた分ずれるため、
    /// リグ原点に当たり判定を置くと「自分は壁際にいるのにスティックで壁を突き抜ける」ことになる。
    ///
    /// テレポートの照準は、プレースホルダ段階では直線レイで出している。
    /// 放物線の弧にするかは部屋のアートを入れるときに見た目と合わせて判断する。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MenuLocomotionView : MonoBehaviour, IMenuLocomotionView
    {
        [SerializeField, Tooltip("HMD（非VRではメインカメラ）。移動方向とカプセル位置の基準にする")]
        private Transform _head;

        [SerializeField, Tooltip("テレポート照準の発射位置。未指定ならHMDから前方へ飛ばす")]
        private Transform _teleportRayOrigin;

        [SerializeField, Tooltip("テレポート照準のライン")]
        private LineRenderer _teleportLine;

        [SerializeField, Tooltip("テレポート着地点のマーカー")]
        private GameObject _teleportMarker;

        [SerializeField, Tooltip("テレポートで着地できる床のレイヤー")]
        private LayerMask _teleportGroundMask = ~0;

        [SerializeField, Min(0f), Tooltip("テレポートできる最大距離[m]")]
        private float _teleportMaxDistance = 12f;

        [SerializeField, Min(0f), Tooltip("接地を保つために毎秒かける下向きの速度[m/s]")]
        private float _gravitySpeed = 9.8f;

        [SerializeField, Min(0.1f), Tooltip("カプセルの最低の高さ[m]。HMDを床近くまで下げても潰れないようにする")]
        private float _minCapsuleHeight = 0.6f;

        /// <summary>床とみなす面の傾き。法線のY成分がこれ以上なら着地できる</summary>
        private const float GroundNormalThreshold = 0.7f;

        private CharacterController _characterController;

        /// <summary>テレポートの照準中か</summary>
        private bool _isAiming;

        /// <summary>照準が床を捉えているか</summary>
        private bool _hasTeleportTarget;

        /// <summary>照準が捉えている着地点（ワールド座標）</summary>
        private Vector3 _teleportTarget;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            SetTeleportAiming(false);
        }

        private void Update()
        {
            SyncCapsuleToHead();

            if (_isAiming)
            {
                UpdateTeleportAim();
            }
        }

        public void Move(Vector2 input, float speed)
        {
            var direction = ToWorldDirection(input);

            // 接地を保つために常に下向きの速度を加える。床は平らな想定なので簡易な落下で足りる
            var motion = direction * speed + Vector3.down * _gravitySpeed;

            _characterController.Move(motion * Time.deltaTime);
        }

        public void SnapTurn(float angleDegrees)
        {
            // リグ原点ではなく頭を軸に回さないと、その場で回ったつもりが body ごと振り回されて酔う。
            // 頭がリグ原点からずれていると回転で位置も動くため、テレポートと同じく
            // CharacterControllerを一時的に切ってから書き換える
            _characterController.enabled = false;
            transform.RotateAround(GetHeadPosition(), Vector3.up, angleDegrees);
            _characterController.enabled = true;
        }

        public void SetTeleportAiming(bool aiming)
        {
            _isAiming = aiming;

            if (!aiming)
            {
                _hasTeleportTarget = false;
            }

            if (_teleportLine != null)
            {
                _teleportLine.enabled = aiming;
            }

            if (_teleportMarker != null)
            {
                _teleportMarker.SetActive(false);
            }
        }

        public bool TeleportToAim()
        {
            if (!_hasTeleportTarget)
            {
                return false;
            }

            // 頭が着地点へ来るようにリグを動かす。リグ原点を合わせると、
            // 実際に歩いた分だけ狙った場所からずれてしまう
            var headOffset = GetHeadPosition() - transform.position;
            headOffset.y = 0f;

            // CharacterControllerを有効にしたまま位置を書き換えると戻されるため、一時的に切る
            _characterController.enabled = false;
            transform.position = _teleportTarget - headOffset;
            _characterController.enabled = true;

            return true;
        }

        /// <summary>当たり判定のカプセルをHMDの位置・高さへ合わせる</summary>
        private void SyncCapsuleToHead()
        {
            if (_head == null)
            {
                return;
            }

            var localHead = transform.InverseTransformPoint(_head.position);
            var height = Mathf.Max(_minCapsuleHeight, localHead.y);

            _characterController.height = height;
            _characterController.center = new Vector3(localHead.x, height * 0.5f, localHead.z);
        }

        /// <summary>テレポート照準のレイを飛ばし、着地点とラインの見た目を更新する</summary>
        private void UpdateTeleportAim()
        {
            var origin = _teleportRayOrigin != null ? _teleportRayOrigin : _head;
            if (origin == null)
            {
                return;
            }

            _hasTeleportTarget = Physics.Raycast(
                                     origin.position,
                                     origin.forward,
                                     out var hit,
                                     _teleportMaxDistance,
                                     _teleportGroundMask,
                                     QueryTriggerInteraction.Ignore)
                                 && hit.normal.y >= GroundNormalThreshold;

            if (_hasTeleportTarget)
            {
                _teleportTarget = hit.point;
            }

            var end = _hasTeleportTarget
                ? _teleportTarget
                : origin.position + origin.forward * _teleportMaxDistance;

            if (_teleportLine != null)
            {
                _teleportLine.SetPosition(0, origin.position);
                _teleportLine.SetPosition(1, end);
            }

            if (_teleportMarker != null)
            {
                _teleportMarker.SetActive(_hasTeleportTarget);

                if (_hasTeleportTarget)
                {
                    _teleportMarker.transform.position = _teleportTarget;
                }
            }
        }

        /// <summary>スティック入力をHMDの向きに合わせた水平方向へ直す</summary>
        private Vector3 ToWorldDirection(Vector2 input)
        {
            if (_head == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            var forward = _head.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < VectorConstants.DirectionEpsilon)
            {
                // 真下（真上）を向いていて水平成分が消えたときは、頭の上方向を前方とみなす
                forward = _head.up;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude < VectorConstants.DirectionEpsilon)
            {
                return Vector3.zero;
            }

            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);

            return right * input.x + forward * input.y;
        }

        private Vector3 GetHeadPosition() => _head != null ? _head.position : transform.position;
    }
}
