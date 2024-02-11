using App.Battle.Interface.Views;
using System.Collections.Generic;
using App.Framework.Utilities;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    public class PlayerShot : MonoBehaviour, IPlayerShot
    {

        private ISimpleObjectFactory<ITestBullet> _bulletFactory;
        private readonly Dictionary<float, ITestBullet> _bulletViews = new();

        [Inject]
        private void Construct(
            ISimpleObjectFactory<ITestBullet> bulletFactory
        )
        {
            _bulletFactory = bulletFactory;
        }

        public void SpawnBullet()
        {
            var bullet = _bulletFactory.Instantiate(transform);

            _bulletViews.Add(Time.time, bullet);

            bullet.Spawn(new (transform.position, transform.rotation));
        }
    }
}