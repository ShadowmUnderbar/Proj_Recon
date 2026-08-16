using App.Battle.Interface;
using App.Framework.Utilities;
using App.Battle.Data;
using App.Framework.Utilities.Extensions;
using UnityEngine;
using VContainer;
using App.Common.Data;

namespace App.Battle.Views
{
    public class PlayerShotView : MonoBehaviour, IPlayerShotView
    {
        private ISimpleObjectFactory<IBulletView> _playerBulletFactory;

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IBulletView> playerBulletFactory
        )
        {
            _playerBulletFactory = playerBulletFactory;
        }

        public void SpawnBullet(BulletData bulletData, int focusTargetId)
        {
            var bullet = _playerBulletFactory.Instantiate(null);

            bullet.Spawn(BasePlayerParameter.PlayerId, transform.ToPose(), bulletData, focusTargetId);
        }

        public void SpawnBulletToward(BulletData bulletData, int focusTargetId, Vector3 targetPosition)
        {
            var bullet = _playerBulletFactory.Instantiate(null);

            // 発射位置は通常の射撃と同じ（手元）で、向きだけ対象へ向ける
            var pose = transform.ToPose();
            var direction = targetPosition - pose.position;

            if (direction.sqrMagnitude > 0f)
            {
                pose.rotation = Quaternion.LookRotation(direction.normalized);
            }

            bullet.Spawn(BasePlayerParameter.PlayerId, pose, bulletData, focusTargetId);
        }
    }
}