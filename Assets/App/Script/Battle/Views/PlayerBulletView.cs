using System.Collections.Generic;
using App.Battle.Interface;
using UnityEngine;
using R3;
using R3.Triggers;
using App.Common.Data;
using App.Framework;
using Cysharp.Threading.Tasks;

namespace App.Battle.Views
{
    public class PlayerBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private Layer _shooterLayer;
        [SerializeField] private Collider _hitCollider;

        private readonly List<int> _hitTargetIds = new();

        private bool _canHit = true;
        private BulletData _bulletData;
        private int _hitCount = 0;
        private int _focusTargetId = 0;
        private int _attackerId = 0;

        public void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId,
            Transform targetTransform = null)
        {
            _attackerId = attackerId;
            _focusTargetId = focusTargetId;

            transform.SetPositionAndRotation(pose.position, pose.rotation);
            _bulletData = bulletData;
            Destroy(gameObject, 5.0f);
        }

        private void Awake()
        {
            _hitCollider.OnTriggerEnterAsObservable()
                .Where(_ => _canHit)
                .Subscribe(HitProcess)
                .AddTo(this);
        }

        private void Update()
        {
            if (!_canHit)
            {
                return;
            }

            transform.position += transform.forward * (_bulletData.Speed * Time.deltaTime);
        }

        private void HitProcess(Collider col)
        {
            if (col.gameObject.layer == _shooterLayer)
            {
                return;
            }

            if (col.gameObject.CompareTag(TagConstants.Bullet))
            {
                return;
            }

            if (!col.TryGetComponent<IHitBoxView>(out var hitBox))
            {
                HitAfterProcess().Forget();
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

            hitBox.OnHit(_bulletData.Damage, _attackerId, transform.position, out var canPenetrable);

            if (_focusTargetId == hitBox.Id)
            {
                HitAfterProcess().Forget();
                return;
            }

            if (!canPenetrable)
            {
                HitAfterProcess().Forget();
                return;
            }

            _hitCount++;

            if (_focusTargetId != default && _hitCount >= _bulletData.Penetration)
            {
                HitAfterProcess().Forget();
            }
        }

        private async UniTask HitAfterProcess()
        {
            _canHit = false;

            if (_trailRenderer == null)
            {
                return;
            }

            await UniTask.WaitForSeconds(_trailRenderer.time);
            Destroy(gameObject);
        }
    }
}