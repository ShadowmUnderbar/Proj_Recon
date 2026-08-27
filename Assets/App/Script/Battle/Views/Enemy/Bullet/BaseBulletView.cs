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

        // 爆風の範囲内判定用バッファと、爆風内で既にダメージを与えた対象のId。
        // 1体が複数のヒットボックスを持つため、Id単位で重複を除外する
        private readonly Collider[] _explosiveHitBuffer = new Collider[32];
        private readonly List<int> _explosiveHitTargetIds = new();

        // 即着弾のヒット結果を距離昇順に並べるための比較子（毎ショットのアロケーション回避のため共有）
        private static readonly IComparer<RaycastHit> _hitDistanceComparer =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        // 即着弾の曳光弾エフェクト生成用ファクトリ（プレイヤー弾のみDIで注入される。敵弾ではnull）
        private ISimpleObjectFactory<BulletTracerView> _tracerFactory;

        [Inject]
        public void Construct(ISimpleObjectFactory<BulletTracerView> tracerFactory)
        {
            _tracerFactory = tracerFactory;
        }

        protected bool CanHit { get; private set; } = true;

        /// <summary>フリーズでその場に止まっているか（移動を止める。当たり判定は生かしたまま）</summary>
        protected bool IsPause { get; private set; }
        protected BulletData BulletData { get; private set; }
        private int _hitCount = 0;
        private int _focusTargetId = 0;
        private int _attackerId = 0;
        private bool _isExploded = false;
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
                return;
            }

            _trailRenderer.enabled = true;
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

        /// <summary>
        /// その場で止める／再開する（フリーズ用）。移動だけを止め、当たり判定と寿命はそのまま。
        /// </summary>
        public void SetPause(bool isPause)
        {
            IsPause = isPause;
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

            // SphereCastNonAllocの結果は距離順が保証されないため、
            // 手前の敵から順にヒット処理する（貫通順序に依存する効果のため）
            System.Array.Sort(_instantHitBuffer, 0, hitCount, _hitDistanceComparer);

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

            // 同一弾内で何体目のヒットか（1始まり）。PenetrationCount条件バフの倍率計算に使う。
            // エイム状態とフォーカス対象一致はキリングコールの条件判定に使う
            // isProjectile: 弾の直撃であることを伝える（回避時跳ね返し攻撃の接触弾カウントに使う）
            // projectileId: この弾を一意に識別するId（同じ弾を重複カウントしないために使う）
            hitBox.OnHit(BulletData.Damage, _attackerId, transform.position, out var canPenetrable,
                _hitTargetIds.Count, BulletData.ShotType, BulletData.FocusType, _focusTargetId == hitBox.Id, true,
                gameObject.GetInstanceID());

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

        /// <summary>
        /// 着弾地点の周囲に爆風ダメージを与える。
        /// 直撃した敵にも重複して入る（直撃 = 弾ダメージ + 爆風ダメージ）。
        /// </summary>
        private void ExplosiveProcess()
        {
            if (_isExploded)
            {
                return;
            }

            if (BulletData.Explosive <= 0f || BulletData.ExplosiveDamage <= 0f)
            {
                return;
            }

            _isExploded = true;

            var origin = transform.position;

            // ヒットボックスがトリガーコライダーの場合も拾うため、明示的にCollideを指定する
            var hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                BulletData.Explosive,
                _explosiveHitBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            _explosiveHitTargetIds.Clear();

            for (var i = 0; i < hitCount; i++)
            {
                var col = _explosiveHitBuffer[i];

                if (col.gameObject.layer == _shooterLayer)
                {
                    continue;
                }

                if (col.gameObject.CompareTag(TagConstants.Bullet))
                {
                    continue;
                }

                if (!col.TryGetComponent<IHitBoxView>(out var hitBox))
                {
                    continue;
                }

                if (hitBox.Id == _attackerId)
                {
                    continue;
                }

                if (_explosiveHitTargetIds.Contains(hitBox.Id))
                {
                    continue;
                }

                _explosiveHitTargetIds.Add(hitBox.Id);

                // 爆風は貫通しないため貫通順は常に1体目扱い、フォーカス対象扱いもしない
                hitBox.OnHit(BulletData.ExplosiveDamage, _attackerId, origin, out _, 1,
                    BulletData.ShotType, BulletData.FocusType);
            }
        }

        private async UniTask HitAfterProcess()
        {
            // 曳光弾の後処理より先に爆風を発生させる（_trailRendererがnullの弾でも爆風は必要）
            ExplosiveProcess();

            if (_trailRenderer == null)
            {
                return;
            }

            if (!CanHit)
            {
                return;
            }

            CanHit = false;

            _trailRenderer.time *= 0.5f;
            // 待機中にgameObjectが破棄された場合（Spawnの5秒自動Destroyやプレイモード終了等）に
            // 破棄済みオブジェクトへアクセスしないよう、destroyCancellationTokenで待機をキャンセルする
            await UniTask.WaitForSeconds(_trailRenderer.time, cancellationToken: destroyCancellationToken);
            Destroy(gameObject);
        }
    }
}