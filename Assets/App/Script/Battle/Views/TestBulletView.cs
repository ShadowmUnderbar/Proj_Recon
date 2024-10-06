using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Data;
using App.Framework.Utilities.Extensions;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using App.Script.Battle.Interface;

namespace App.Battle.Views
{
    public class TestBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private Collider _hitCollider;

        public IObservable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        private readonly List<uint> _hitTargetIds = new();

        private BulletData _bulletData;
        private int _hitCount = 0;

        public void Spawn(Pose pose, BulletData bulletData)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _bulletData = bulletData;
            Destroy(gameObject, 5.0f);
        }

        private void Awake()
        {
            _hitCollider.OnTriggerEnterAsObservable()
                .Synchronize()
                .Subscribe(x =>
                {
                    if (!x.TryGetComponent<IEnemyView>(out var enemyView))
                    {
                        return;
                    }

                    if (_hitTargetIds.Contains(enemyView.Id))
                    {
                        return;
                    }

                    _hitTargetIds.Add(enemyView.Id);

                    var hitData = new HitData
                    {
                        DamagedId = enemyView.Id,
                        Damage = _bulletData.Damage,
                        NormalizedHitDirection = (x.transform.position - transform.position).normalized.ToTopdown(),
                    };
                    _onHit.OnNext(hitData);

                    _hitCount++;
                    
                    if (_hitCount >= _bulletData.Penetration)
                    {
                        Destroy(gameObject);
                    }
                })
                .AddTo(this);
        }

        private void Update()
        {
            transform.position += transform.forward * _bulletData.Speed * Time.deltaTime;
        }

        private void OnDestroy()
        {
            _onHit.Dispose();
        }
    }
}