using App.Battle.Interface;
using System.Collections.Generic;
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
        private readonly Dictionary<float, IBulletView> _bulletViews = new();

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

            _bulletViews.Add(Time.time, bullet);
            bullet.Spawn(BasePlayerParameter.PlayerId, transform.ToPose(), bulletData, focusTargetId);
        }
    }
}