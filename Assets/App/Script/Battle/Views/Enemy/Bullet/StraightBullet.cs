using App.Common.Data;
using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    public class StraightBullet : BaseBulletView
    {
        public override void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId,
            Transform targetTransform = null)
        {
            base.Spawn(attackerId, pose, bulletData, focusTargetId, targetTransform);

            Debug.Log("StraightBullet:Spawn");
            Destroy(gameObject, 5.0f);
        }

        protected override void Update()
        {
            transform.position += transform.forward * (Speed * Time.deltaTime);
        }
    }
}