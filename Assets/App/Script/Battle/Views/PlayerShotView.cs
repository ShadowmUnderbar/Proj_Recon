using System;
using App.Battle.Interface.Views;
using System.Collections.Generic;
using App.Framework.Utilities;
using App.Script.Battle.Data;
using App.Script.Framework.Utilities.Extensions;
using UniRx;
using UnityEngine;
using VContainer;

namespace App.Battle.Views
{
    public class PlayerShotView : MonoBehaviour, IPlayerShotView
    {
        IObservable<HitData> IPlayerShotView.OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        private ISimpleObjectFactory<IBulletView> _bulletFactory;
        private readonly Dictionary<float, IBulletView> _bulletViews = new();

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