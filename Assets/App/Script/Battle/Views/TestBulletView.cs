using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Data;
using App.Framework.Utilities.Extensions;
using UnityEngine;
using R3;
using R3.Triggers;
using App.Common.Data;

namespace App.Battle.Views
{
    public class TestBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private Collider _hitCollider;

        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        private readonly List<uint> _hitTargetIds = new();

        private BulletData _bulletData;
        private int _hitCount = 0;
        private int _focusTargetId = 0;
        private bool _isForcedPenetration = false;

        public void Spawn(Pose pose, BulletData bulletData, int focusTargetId)
        {
            _isForcedPenetration = focusTargetId >= 0;

            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _bulletData = bulletData;
            Destroy(gameObject, 5.0f);
        }

        private void Awake()
        {
            _hitCollider.OnTriggerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (x.gameObject.CompareTag("Player"))
                    {
                        return;
                    }

                    if (!x.TryGetComponent<IEnemyView>(out var enemyView))
                    {
                        Destroy(gameObject);
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

                    if (_focusTargetId == enemyView.Id)
                    {
                        _isForcedPenetration = false;
                    }

                    if (_isForcedPenetration)
                    {
                        return;
                    }

                    _hitCount++;

                    if (_isForcedPenetration && _hitCount >= _bulletData.Penetration)
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