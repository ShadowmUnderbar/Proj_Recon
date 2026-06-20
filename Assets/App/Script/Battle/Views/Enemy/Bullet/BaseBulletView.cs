using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Views;
using App.Common.Data;
using App.Framework;
using App.Framework.Utilities;
using Cysharp.Threading.Tasks;
using R3;
using R3.Triggers;
using UnityEngine;
using VContainer;

namespace App.Battle.Views.Enemy.Bullet
{
    public class BaseBulletView : MonoBehaviour, IBulletView
    {
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private Layer _shooterLayer;
        [SerializeField] private Collider _hitCollider;

        // 即着弾で何にも当たらなかった場合にトレーサーを伸ばす最大距離
        [SerializeField] private float _maxTracerDistance = 50f;

        private readonly List<int> _hitTargetIds = new();
        private readonly RaycastHit[] _instantHitBuffer = new RaycastHit[10];

        // 即着弾の曳光弾エフェクト生成用ファクトリ（プレイヤー弾のみDIで注入される。敵弾ではnull）
        private ISimpleObjectFactory<BulletTracerView> _tracerFactory;

        [Inject]
        public void Construct(ISimpleObjectFactory<BulletTracerView> tracerFactory)
        {
            _tracerFactory = tracerFactory;
        }

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
            // 弾はまだ移動していないため、現在位置が発射地点
            var origin = transform.position;

            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                BulletData.Size * 0.5f,
                transform.forward,
                _instantHitBuffer,
                Mathf.Infinity);

            // トレーサーの着弾地点を算出（shooterレイヤー/弾タグを除外した最遠の有効ヒット）
            var maxHitDistance = -1f;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _instantHitBuffer[i];
                if (hit.collider.gameObject.layer == _shooterLayer)
                {
                    continue;
                }

                if (hit.collider.gameObject.CompareTag(TagConstants.Bullet))
                {
                    continue;
                }

                if (hit.distance > maxHitDistance)
                {
                    maxHitDistance = hit.distance;
                }
            }

            // 有効ヒットが無ければ最大距離まで線を伸ばす
            var endPos = maxHitDistance >= 0f
                ? origin + transform.forward * maxHitDistance
                : origin + transform.forward * _maxTracerDistance;

            SpawnTracer(origin, endPos);

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

        private void SpawnTracer(Vector3 startPos, Vector3 endPos)
        {
            // 敵弾などファクトリ未注入の場合はトレーサーを出さない
            if (_tracerFactory == null)
            {
                return;
            }

            // 元の弾と同じマテリアルをトレーサーに使わせる
            var material = _trailRenderer != null ? _trailRenderer.sharedMaterial : null;

            var tracer = _tracerFactory.Instantiate(null);
            // 線の太さは弾の当たり判定サイズ(SphereCast直径 = BulletData.Size)に合わせる
            tracer.Play(startPos, endPos, material, BulletData.Size).Forget();
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