using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views
{
    // プレイヤーモデルの向き（エイム中心への振り向き）とAnimatorの移動ブレンドパラメータを扱うView
    public class PlayerAnimationView : MonoBehaviour, IPlayerAnimationView
    {
        [SerializeField] private Animator _animator;

        // 回転対象のモデルTransform。未割当の場合はAwakeで_animator.transformを採用
        [SerializeField] private Transform _modelTransform;

        private const string MoveXParam = "MoveX";
        private const string MoveYParam = "MoveY";

        // 急な方向転換でもモーションが滑らかに切り替わるようダンピングを掛ける
        [SerializeField] private float _dampTime = 0.1f;

        // 振り向き速度（度/秒）
        [SerializeField] private float _rotationSpeed = 360f;

        // 必要回転がこの角度以上の間は振り向き先を保留（180°付近の急反転を防ぐ）。
        // 既定は180°超とし、中心方向が定義されている限り常に振り向く（腕が後方へ伸びるのを防ぐ）。
        // 真後ろの曖昧ケースは CenterAimDirection が Vector3.zero を返して別途保留される。
        [SerializeField] private float _turnLockAngle = 200f;

        // 毎フレームの文字列参照を避けるためハッシュをキャッシュ
        private int _moveXHash;
        private int _moveYHash;

        private Quaternion _targetFacing;
        private bool _hasFacing;

        private void Awake()
        {
            _moveXHash = Animator.StringToHash(MoveXParam);
            _moveYHash = Animator.StringToHash(MoveYParam);

            if (_modelTransform == null && _animator != null)
            {
                _modelTransform = _animator.transform;
            }
        }

        public void SetFacingDirection(Vector3 dir)
        {
            if (_modelTransform == null)
            {
                return;
            }

            // 中心方向が微小（両手が正反対付近で不安定）なときは振り向き先を更新しない
            if (dir.sqrMagnitude > Mathf.Epsilon)
            {
                var desired = Quaternion.LookRotation(dir, Vector3.up);

                if (!_hasFacing)
                {
                    _targetFacing = desired;
                    _hasFacing = true;
                }
                // 必要回転が_turnLockAngle未満なら追従、以上なら保留（180°付近ロック）
                else if (Quaternion.Angle(_modelTransform.rotation, desired) < _turnLockAngle)
                {
                    _targetFacing = desired;
                }
            }

            if (_hasFacing)
            {
                _modelTransform.rotation = Quaternion.RotateTowards(
                    _modelTransform.rotation,
                    _targetFacing,
                    _rotationSpeed * Time.deltaTime
                );
            }
        }

        // ワールド入力をモデルの現在の向き基準にローカル化してブレンドパラメータへ反映する
        public void SetMoveDirection(Vector2 worldMove)
        {
            // モデル/Animator未割当時のガード
            if (_animator == null || _modelTransform == null)
            {
                return;
            }

            var world = new Vector3(worldMove.x, 0, worldMove.y);
            var local = _modelTransform.InverseTransformDirection(world);

            _animator.SetFloat(_moveXHash, local.x, _dampTime, Time.deltaTime);
            _animator.SetFloat(_moveYHash, local.z, _dampTime, Time.deltaTime);
        }
    }
}
