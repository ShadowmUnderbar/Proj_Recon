using App.Battle.Data;
using App.Battle.Interface.EnemyAI;
using App.Battle.Views.Enemy.Bullet;
using App.Framework.Utilities.Extensions;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Orbit : EnemyAIBase
    {
        [SerializeField] private StraightBullet _bulletPrefab;

        private bool _isRotateRight;

        protected override void Awake()
        {
            base.Awake();

            _isRotateRight = Random.Range(0, 2) == 0;
        }

        protected override void OnUpdateIdleState()
        {
            base.OnUpdateIdleState();

            Agent.updateRotation = true;
        }

        protected override void OnUpdateBattleState()
        {
            base.OnUpdateBattleState();

            Agent.updateRotation = false;
        }

        protected override void IdleState()
        {
            base.IdleState();

            Agent.SetDestination(PlayerPose.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            transform.LookAt(PlayerPose.position, Vector3.up);
            var targetPos = transform.right * EnemyData.BattleSpeed * (_isRotateRight ? 1 : -1);

            if (IsChaseRange())
            {
                targetPos += transform.forward * EnemyData.BattleSpeed * 0.5f;
            }

            Agent.SetDestination(targetPos);
        }

        private bool IsChaseRange()
        {
            return DistanceSqr <= EnemyData.AttackDistanceRange * EnemyData.AttackDistanceRange * 0.5f;
        }

        protected override void Attack()
        {
            base.Attack();

            var bullet = Instantiate(_bulletPrefab);
            bullet.Spawn(BasePlayerParameter.PlayerId, transform.ToPose(), EnemyData.BulletData, -1);
        }
    }
}