using System.Collections.Generic;
using App.Battle.Interface;
using UnityEngine;
using R3;
using R3.Triggers;
using App.Common.Data;

namespace App.Battle.Views
{
    public class TestBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private Collider _hitCollider;

        private readonly List<int> _hitTargetIds = new();

        private BulletData _bulletData;
        private int _hitCount = 0;
        private int _focusTargetId = 0;
        private bool _isForcedPenetration = false;
        private int _attackerId = 0;

        public void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId)
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

                    if (x.gameObject.CompareTag($"Bullet"))
                    {
                        return;
                    }

                    if (!x.TryGetComponent<IHitBoxView>(out var hitBox))
                    {
                        Destroy(gameObject);
                        return;
                    }

                    if (hitBox.Id == _attackerId)
                    {
                        return;
                    }

                    if (_hitTargetIds.Contains(hitBox.Id))
                    {
                        return;
                    }

                    _hitTargetIds.Add(hitBox.Id);

                    hitBox.OnHit(_bulletData.Damage, _attackerId, transform.position);

                    if (_focusTargetId == hitBox.Id)
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
            transform.position += transform.forward * (_bulletData.Speed * Time.deltaTime);
        }
    }
}