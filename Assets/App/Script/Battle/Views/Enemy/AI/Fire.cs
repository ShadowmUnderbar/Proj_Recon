using App.Battle.Interface.EnemyAI;
using App.Battle.Views.Enemy.Bullet;
using App.Common.Data;
using App.Framework.Utilities.Extensions;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Fire : EnemyAIBase
    {
        [SerializeField] private StraightBullet _bulletPrefab;
        [SerializeField] private Transform _muzzleTransform;

        protected Transform MuzzleTransform => _muzzleTransform;

        protected override void Attack()
        {
            base.Attack();

            var bullet = Instantiate(_bulletPrefab);

            var bulletData = new BulletData
            {
                ShotType = ShotType.Normal,
                FocusType = AimFocusType.NotFocus,
                Damage = EnemyData.BaseDamage,
                Speed = EnemyData.BaseBulletSpeed,
                Penetration = 0,
                Explosive = 0
            };

            bullet.Spawn(EnemyId, _muzzleTransform.ToPose(), bulletData, -1);
        }
    }
}