using System;
using App.Battle.Interface;
using System.Collections.Generic;
using App.Framework.Utilities;
using App.Battle.Data;
using App.Framework.Utilities.Extensions;
using UniRx;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    public class PlayerShotView : MonoBehaviour, IPlayerShotView
    {
        private ISimpleObjectFactory<IBulletView> _bulletFactory;
        private readonly Dictionary<float, IBulletView> _bulletViews = new();

        public IObservable<HitData> OnHit => _onHit;
        private Subject<HitData> _onHit = new Subject<HitData>();

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IBulletView> bulletFactory
        )
        {
            _bulletFactory = bulletFactory;
        }

        public void SpawnBullet()
        {
            var bullet = _bulletFactory.Instantiate(null);

            _bulletViews.Add(Time.time, bullet);

            bullet.Spawn(transform.ToPose(), new BulletData());
            bullet.OnHit.Subscribe(_onHit).AddTo(this);
        }
    }
}