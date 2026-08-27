using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    public class HomingBullet : BaseBulletView
    {
        [SerializeField] private float _rotationSpeed = 20f;

        private Transform _homingTarget;

        public override void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId,
            Transform targetTransform = null)
        {
            base.Spawn(attackerId, pose, bulletData, focusTargetId, targetTransform);

            _homingTarget = targetTransform;
        }

        protected override void Update()
        {
            base.Update();

            if (!CanHit || IsPause)
            {
                return;
            }

            transform.position += transform.forward * (BulletData.Speed * Time.deltaTime);

            if (_homingTarget == null)
            {
                return;
            }

            var direction = _homingTarget.position - transform.position;

            var targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }
    }
}