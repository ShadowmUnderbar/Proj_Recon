using App.Battle.Interface;
using System.Collections.Generic;
using App.Framework.Utilities;
using App.Battle.Data;
using App.Framework.Utilities.Extensions;
using UnityEngine;
using VContainer;
using R3;
using App.Common.Data;
using App.Common.Data.Database;

namespace App.Battle.Views
{
    public class PlayerShotView : MonoBehaviour, IPlayerShotView
    {
        private ISimpleObjectFactory<IBulletView> _bulletFactory;
        private readonly Dictionary<float, IBulletView> _bulletViews = new();

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IBulletView> bulletFactory
        )
        {
            _bulletFactory = bulletFactory;
        }

        public void SpawnBullet(BulletData bulletData, int focusTargetId)
        {
            var bullet = _bulletFactory.Instantiate(null);

            _bulletViews.Add(Time.time, bullet);
            bullet.Spawn(BasePlayerParameter.PlayerId, transform.ToPose(), bulletData, focusTargetId);
        }
    }
}