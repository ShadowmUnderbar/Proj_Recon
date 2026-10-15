using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// サモンタレット。その場から動かず、射程内で最も近い敵へ旋回速度（AllyData.TurnSpeed）で砲身を向け、
    /// 向き切ったら攻撃間隔ごとに撃つ
    /// </summary>
    public class AllyTurretView : AllyViewBase
    {
        [SerializeField, Tooltip("水平に旋回して敵へ向ける部分")]
        private Transform _head;

        [SerializeField, Tooltip("弾を撃ち出す位置")]
        private Transform _muzzle;

        [SerializeField, Min(0f), Tooltip("砲身の向きと敵の方向の差がこの角度[度]以内なら撃つ")]
        private float _fireAngle = 5f;

        [SerializeField, Min(0.02f), Tooltip("狙う敵（最も近い敵）を探し直す間隔[秒]")]
        private float _searchInterval = 0.25f;

        private readonly AllyEnemySearcher _enemySearcher = new();

        // 前回撃ってからの経過秒数。撃てない間（敵がいない・向き直り中）は溜めたままにし、撃てるようになった瞬間に撃つ
        private float _attackElapsed;

        // 前回敵を探してからの経過秒数
        private float _searchElapsed;

        // 狙っている敵のヒットボックス。動く敵を追えるよう、位置ではなくコライダーを持つ
        private Collider _target;

        // 撃つと決めた瞬間の狙う位置（攻撃の要求から弾データを受け取るまでの間に使う）
        private Vector3 _shotTargetPosition;

        private void Update()
        {
            if (AllyData == null || IsPause)
            {
                return;
            }

            _attackElapsed += Time.deltaTime;

            UpdateTarget();
            if (_target == null)
            {
                return;
            }

            var targetPosition = _target.bounds.center;
            var isFacing = TurnToward(targetPosition);

            if (!isFacing || _attackElapsed < AllyData.AttackInterval)
            {
                return;
            }

            _attackElapsed = 0f;
            _shotTargetPosition = targetPosition;
            RequestShot();
        }

        /// <summary>
        /// 一定間隔で、または狙っている敵がいなくなったら、射程内で最も近い敵を探し直す
        /// </summary>
        private void UpdateTarget()
        {
            _searchElapsed += Time.deltaTime;

            var isTargetLost = _target == null || !_target.enabled ||
                               (_target.bounds.center - _muzzle.position).sqrMagnitude >
                               AllyData.AttackRange * AllyData.AttackRange;

            if (!isTargetLost && _searchElapsed < _searchInterval)
            {
                return;
            }

            _searchElapsed = 0f;
            _enemySearcher.TryFindNearest(_muzzle.position, AllyData.AttackRange, out _target);
        }

        /// <summary>
        /// 砲身を敵の方向へ旋回速度で水平に回す。向きの差が射撃角度以内になったら true
        /// </summary>
        private bool TurnToward(Vector3 targetPosition)
        {
            if (_head == null)
            {
                return true;
            }

            var direction = targetPosition - _head.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f)
            {
                return true;
            }

            var targetRotation = Quaternion.LookRotation(direction);

            // 0 は「即座に向く」
            _head.rotation = AllyData.TurnSpeed <= 0f
                ? targetRotation
                : Quaternion.RotateTowards(_head.rotation, targetRotation, AllyData.TurnSpeed * Time.deltaTime);

            return Quaternion.Angle(_head.rotation, targetRotation) <= _fireAngle;
        }

        public override void Shot(BulletData bulletData)
        {
            var direction = _shotTargetPosition - _muzzle.position;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            FireBullet(new Pose(_muzzle.position, Quaternion.LookRotation(direction)), bulletData);
        }
    }
}
