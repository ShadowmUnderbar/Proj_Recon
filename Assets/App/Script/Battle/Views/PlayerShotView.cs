using App.Battle.Interface.Views;
using System.Collections.Generic;
using App.Framework.Utilities;
using UnityEngine;
using VContainer;

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
            SpawnBullet();
        }

        public void SpawnBullet()
        {
            var bullet = _bulletFactory.Instantiate(null);

            _bulletViews.Add(Time.time, bullet);

            bullet.Spawn(new (transform.position, transform.rotation));
        }
    }
}