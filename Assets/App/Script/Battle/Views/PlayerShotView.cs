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
        private BulletDataBase _bulletDataBase;
        private ISimpleObjectFactory<IBulletView> _bulletFactory;
        private readonly Dictionary<float, IBulletView> _bulletViews = new();

        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        [Inject]
        public void Construct(
            ISimpleObjectFactory<IBulletView> bulletFactory,
            BulletDataBase bulletDataBase
        )
        {
            _bulletFactory = bulletFactory;
            _bulletDataBase = bulletDataBase;
        }

        public void SpawnBullet(ShotType shotType, AimFocusType focusType, int focusTargetId)
        {
            if (!_bulletDataBase.TryGetBulletData(shotType, focusType, out var bulletData))
            {
                return;
            }

            var bullet = _bulletFactory.Instantiate(null);

            _bulletViews.Add(Time.time, bullet);
            bullet.Spawn(transform.ToPose(), bulletData, focusTargetId);
            bullet.OnHit.Subscribe(x => _onHit.OnNext(x)).AddTo(this);
        }
    }
}