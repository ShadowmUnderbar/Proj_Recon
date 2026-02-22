using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    public class StraightBullet : BaseBulletView
    {
        protected override void Update()
        {
            base.Update();

            if (!CanHit)
            {
                return;
            }

            transform.position += transform.forward * (BulletData.Speed * Time.deltaTime);
        }
    }
}