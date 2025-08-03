using App.Battle.Interface.EnemyAI;
using App.Battle.Views.Enemy.Bullet;
using App.Framework.Utilities.Extensions;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Orbit : EnemyAIBase
    {
        [SerializeField] private StraightBullet _bulletPrefab;
        [SerializeField] private Transform _muzzleTransform;

        private bool _isRotateRight;

        private bool IsChaseRange =>
            DistanceSqr > EnemyData.AttackDistanceRange * 0.75f * EnemyData.AttackDistanceRange * 0.75f;

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
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

            var targetPos = transform.right * EnemyData.BattleSpeed * 0.5f * (_isRotateRight ? 1 : -1) * 10f;
            targetPos += transform.forward * EnemyData.BattleSpeed * (IsChaseRange ? 1 : -1) * 10f;

            Agent.SetDestination(targetPos);
        }

        protected override void Attack()
        {
            base.Attack();

            var bullet = Instantiate(_bulletPrefab);
            bullet.Spawn(EnemyId, _muzzleTransform.ToPose(), EnemyData.BulletData, -1);
        }
    }
}