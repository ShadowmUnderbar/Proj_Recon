using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// サモンタレット。その場から動かず、攻撃間隔ごとに射程内で最も近い敵へ砲身を向けて撃つ
    /// </summary>
    public class AllyTurretView : AllyViewBase
    {
        [SerializeField, Tooltip("水平に旋回して敵へ向ける部分")]
        private Transform _head;

        [SerializeField, Tooltip("弾を撃ち出す位置")]
        private Transform _muzzle;

        private readonly AllyEnemySearcher _enemySearcher = new();

        // 前回撃ってからの経過秒数。射程内に敵がいなければ溜めたままにし、敵が入った瞬間に撃てるようにする
        private float _attackElapsed;

        private Vector3 _targetPosition;

        private void Update()
        {
            if (AllyData == null || IsPause)
            {
                return;
            }

            _attackElapsed += Time.deltaTime;
            if (_attackElapsed < AllyData.AttackInterval)
            {
                return;
            }

            if (!_enemySearcher.TryFindNearest(_muzzle.position, AllyData.AttackRange, out var targetPosition))
            {
                return;
            }

            _attackElapsed = 0f;
            _targetPosition = targetPosition;
            FaceTarget(targetPosition);
            RequestShot();
        }

        public override void Shot(BulletData bulletData)
        {
            var direction = _targetPosition - _muzzle.position;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            FireBullet(new Pose(_muzzle.position, Quaternion.LookRotation(direction)), bulletData);
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            if (_head == null)
            {
                return;
            }

            var direction = targetPosition - _head.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            _head.rotation = Quaternion.LookRotation(direction);
        }
    }
}
