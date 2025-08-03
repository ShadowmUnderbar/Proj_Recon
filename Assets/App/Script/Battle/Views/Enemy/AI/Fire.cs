using App.Battle.Interface.EnemyAI;
using App.Battle.Views.Enemy.Bullet;
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
            bullet.Spawn(EnemyId, _muzzleTransform.ToPose(), EnemyData.BulletData, -1);
        }
    }
}