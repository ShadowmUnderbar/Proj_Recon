using System.Collections.Generic;
using App.Battle.Interface;
using App.Common.Data;
using App.Framework;
using Cysharp.Threading.Tasks;
using R3;
using R3.Triggers;
using UnityEngine;

namespace App.Battle.Views.Enemy.Bullet
{
    public class BaseBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private Layer _shooterLayer;
        [SerializeField] private Collider _hitCollider;

        private readonly List<int> _hitTargetIds = new();
        private readonly RaycastHit[] _instantHitBuffer = new RaycastHit[10];

        protected bool CanHit { get; private set; } = true;
        protected BulletData BulletData { get; private set; }
        private int _hitCount = 0;
        private int _focusTargetId = 0;
        private int _attackerId = 0;
        protected Transform TargetTransform;

        public virtual void Spawn(int attackerId, Pose pose, BulletData bulletData, int focusTargetId,
            Transform targetTransform = null)
        {
            _attackerId = attackerId;
            _focusTargetId = focusTargetId;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            BulletData = bulletData;
            transform.localScale = Vector3.one * BulletData.Size;
            Destroy(gameObject, 5.0f);
            if (bulletData.Speed <= 0)
            {
                InstantHitCheck();
            }
        }

        protected virtual void Awake()
        {
            _hitCollider.OnTriggerEnterAsObservable()
                .Where(_ => CanHit)
                .Subscribe(HitProcess)
                .AddTo(this);
        }

        protected virtual void Update()
        {
        }

        private void InstantHitCheck()
        {
            var hitCount = Physics.SphereCastNonAlloc(
                transform.position,
                BulletData.Size * 0.5f,
                transform.forward,
                _instantHitBuffer,
                Mathf.Infinity);

            for (var i = 0; i < hitCount; i++)
            {
                if (!CanHit) break;
                HitProcess(_instantHitBuffer[i].collider);
            }

            if (CanHit)
            {
                HitAfterProcess().Forget();
            }
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

            hitBox.OnHit(BulletData.Damage, _attackerId, transform.position, out var canPenetrable);

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

            if (_focusTargetId != default && _hitCount >= BulletData.Penetration)
            {
                HitAfterProcess().Forget();
            }
        }

        private async UniTask HitAfterProcess()
        {
            CanHit = false;

            if (_trailRenderer == null)
            {
                return;
            }

            var time = _trailRenderer.time *= 0.5f;
            _trailRenderer.time = time;
            await UniTask.WaitForSeconds(time);
            Destroy(gameObject);
        }
    }
}