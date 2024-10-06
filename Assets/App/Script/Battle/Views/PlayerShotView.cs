using App.Battle.Interface;
using System.Collections.Generic;
using App.Framework.Utilities;
using App.Battle.Data;
using App.Framework.Utilities.Extensions;
using UnityEngine;
using VContainer;
using R3;

namespace App.Battle.Views
{
    public class PlayerShotView : MonoBehaviour, IPlayerShotView
    {
        private ISimpleObjectFactory<IBulletView> _bulletFactory;
        private readonly Dictionary<float, IBulletView> _bulletViews = new();

        public Observable<HitData> OnHit => _onHit;
        private Subject<HitData> _onHit = new Subject<HitData>();

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IBulletView> bulletFactory
        )
        {
            _bulletFactory = bulletFactory;
        }

        public void SpawnBullet(ShotType shotType)
        {
            var bullet = _bulletFactory.Instantiate(null);

            _bulletViews.Add(Time.time, bullet);

            bullet.Spawn(transform.ToPose(), new BulletData());
            bullet.OnHit.Subscribe(x => _onHit.OnNext(x)).AddTo(this);
        }
    }
}