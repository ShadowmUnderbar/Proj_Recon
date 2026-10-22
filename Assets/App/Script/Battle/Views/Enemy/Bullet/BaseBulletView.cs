using System.Collections.Generic;
using System.Threading;
using App.Battle.Data;
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
    /// <summary>
    /// 弾の共通処理。プールから貸し出されて使い回されるため、状態は <see cref="OnRent"/> で毎回初期化し、
    /// 役目を終えたら Destroy ではなく <see cref="ReturnToPool"/> でプールへ戻る。
    /// 着弾後の待機は貸出ごとのトークンで打ち切る（返却後に再貸出された弾を古い待機が消さないように）
    /// </summary>
    public class BaseBulletView : MonoBehaviour, IBulletView, IPoolableView
    {
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private Layer _shooterLayer;
        [SerializeField] private Collider _hitCollider;

        // 即着弾で何にも当たらなかった場合にトレーサーを伸ばす最大距離
        [SerializeField] private float _maxTracerDistance = 50f;

        private readonly List<int> _hitTargetIds = new();

        // 即着弾の判定（バッファを使い回す plain C#）
        private readonly HitscanResolver _hitscan = new();

        // 爆風の範囲内判定用バッファと、爆風内で既にダメージを与えた対象のId。
        // 1体が複数のヒットボックスを持つため、Id単位で重複を除外する
        private readonly Collider[] _explosiveHitBuffer = new Collider[32];

        private readonly List<int> _explosiveHitTargetIds = new();

        // 弾の寿命（秒）。停止中は減らさない
        private const float LifetimeSeconds = 5.0f;

        // 軌跡を残している間のトレイルの寿命（秒）。オーバークロックは数秒なので、実質消えない長さにする
        private const float HeldTrailTimeSeconds = 3600f;

        // 即着弾の曳光弾エフェクト生成用ファクトリ（プレイヤー弾のみDIで注入される。敵弾ではnull）
        private ISimpleObjectFactory<BulletTracerView> _tracerFactory;

        // レイ停止の共有状態。オーバークロック中に生まれた弾へ停止を伝えるために見る（敵弾ではnull）
        private ITracerFreezeState _tracerFreezeState;

        [Inject]
        public void Construct(
            ISimpleObjectFactory<BulletTracerView> tracerFactory,
            ITracerFreezeState tracerFreezeState
        )
        {
            _tracerFactory = tracerFactory;
            _tracerFreezeState = tracerFreezeState;
        }

        protected bool CanHit { get; private set; } = true;

        // フリーズで止めているか（自弾・敵弾とも）
        private bool _isFreezePause;

        // オーバークロックで止めているか（敵弾のみ。自弾は飛び続ける）
        private bool _isOverclockPause;

        // オーバークロック中か。自弾・敵弾ともに寿命とトレイルを止め、軌跡を消さない
        private bool _isOverclockHold;

        // 停止していないときのトレイルの寿命（着弾後の縮退もこの値に反映する）
        private float _trailTime;

        private float _remainingLifetime = LifetimeSeconds;

        // プレハブ設定のトレイル寿命と表示状態（着弾後に縮めるので、貸出のたびに戻す）
        private float _trailTimeDefault;
        private bool _trailEnabledDefault;

        // 返却先のプール。プールを通さず生成された弾（null）は従来どおり Destroy する
        private IViewReturner _returner;

        // 着弾後の待機を打ち切るためのトークン（貸出ごとに作り直す）
        private CancellationTokenSource _rentCts;

        /// <summary>貸出ごとの通し番号。命中の重複判定が「同じ弾か」を見分けるのに使う（再利用で InstanceID は当てにならない）</summary>
        public int BulletId { get; private set; }

        /// <summary>プールから貸し出されて場にいるか</summary>
        public bool IsRented { get; private set; }

        // 命中の重複判定へ渡す弾の識別子。プールを通さず生成した弾（通し番号なし＝0）は InstanceID で代用する
        private int ProjectileId => BulletId != 0 ? BulletId : gameObject.GetInstanceID();

        /// <summary>その場に止まっているか（移動を止める。当たり判定は生かしたまま）</summary>
        protected bool IsPause => _isFreezePause || _isOverclockPause;

        // 寿命・着弾後の待機を進めないか
        private bool IsTimeHold => IsPause || _isOverclockHold;

        // プレイヤー側（プレイヤー・召喚した味方）の弾か。撃ち手の Id ではなく、弾がすり抜けるレイヤーで見る
        private bool IsPlayerSideBullet => ((1 << _shooterLayer.Value) & LayerConstants.Player) != 0;
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
            _remainingLifetime = LifetimeSeconds;

            // 前回の位置から線が伸びないよう、置き直してから軌跡を消す
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }

            // オーバークロック中に撃たれた自弾も、発動中は軌跡を残す（解除はBulletStoreViewが一括で行う）
            if (_tracerFreezeState is { IsOverclock: true })
            {
                SetOverclock(true);
            }

            if (bulletData.Speed <= 0)
            {
                InstantHitCheck();
                return;
            }

            _trailRenderer.enabled = true;
        }

        protected virtual void Awake()
        {
            _trailTimeDefault = _trailRenderer != null ? _trailRenderer.time : 0f;
            _trailEnabledDefault = _trailRenderer != null && _trailRenderer.enabled;
            _trailTime = _trailTimeDefault;

            _hitCollider.OnTriggerEnterAsObservable()
                .Where(_ => CanHit)
                .Subscribe(HitProcess)
                .AddTo(this);
        }

        protected virtual void Update()
        {
            // 止まっている間は寿命を減らさない（止まった弾が停止中に消えないように）
            if (IsTimeHold)
            {
                return;
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime > 0f)
            {
                return;
            }

            ReturnToPool();
        }

        /// <summary>プールから貸し出された直後。前回の弾の状態を全て捨てる</summary>
        public virtual void OnRent(IViewReturner returner)
        {
            _returner = returner;
            IsRented = true;
            _rentCts = new CancellationTokenSource();

            CanHit = true;
            _hitTargetIds.Clear();
            _explosiveHitTargetIds.Clear();
            _hitCount = 0;
            _focusTargetId = 0;
            _attackerId = 0;
            _isExploded = false;
            _isFreezePause = false;
            _isOverclockPause = false;
            _isOverclockHold = false;
            TargetTransform = null;
            _remainingLifetime = LifetimeSeconds;

            _trailTime = _trailTimeDefault;
            if (_trailRenderer != null)
            {
                _trailRenderer.time = _trailTimeDefault;
                _trailRenderer.enabled = _trailEnabledDefault;
                _trailRenderer.Clear();
            }
        }

        /// <summary>プールへ戻る直前。着弾後の待機を打ち切り、軌跡を消す</summary>
        public virtual void OnReturn()
        {
            IsRented = false;
            CanHit = false;

            _rentCts?.Cancel();
            _rentCts?.Dispose();
            _rentCts = null;

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.enabled = _trailEnabledDefault;
            }
        }

        /// <summary>貸出ごとの通し番号を付ける（<see cref="IBulletSpawner"/> が貸出時に呼ぶ）</summary>
        public void AssignBulletId(int bulletId)
        {
            BulletId = bulletId;
        }

        /// <summary>役目を終えた弾をプールへ戻す。プールを通していない弾は Destroy する</summary>
        public void ReturnToPool()
        {
            if (_returner != null)
            {
                if (IsRented)
                {
                    _returner.Return(this);
                }

                return;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // シーン終了などでプールごと消えるとき、待機中の処理を止める
            _rentCts?.Cancel();
            _rentCts?.Dispose();
            _rentCts = null;
        }

        /// <summary>
        /// その場で止める／再開する（フリーズ用）。移動と寿命を止め、当たり判定は生かしたまま。
        /// </summary>
        public void SetPause(bool isPause)
        {
            _isFreezePause = isPause;
        }

        /// <summary>
        /// オーバークロックの開始・終了を反映する。
        /// 敵弾はその場で止め、プレイヤー側の弾（自弾・味方の弾）は飛ばし続ける。どちらも寿命とトレイルを止めて軌跡を残す。
        /// </summary>
        public void SetOverclock(bool isActive)
        {
            _isOverclockHold = isActive;
            _isOverclockPause = isActive && !IsPlayerSideBullet;

            if (_trailRenderer == null)
            {
                return;
            }

            // 止まっている弾のトレイルも時間で点が消えていくため、発動中は寿命を実質無限にする。
            // 終了時に元へ戻すと、古い点はその場で消える
            _trailRenderer.time = isActive ? HeldTrailTimeSeconds : _trailTime;
        }

        private void InstantHitCheck()
        {
            // 弾はまだ移動していないため、現在位置が発射地点
            var origin = transform.position;

            // 判定本体は HitscanResolver（ヒットを手前から順に並べ、曳光弾の終点も求める）
            _hitscan.Cast(origin, transform.forward, BulletData.Size, _shooterLayer.Value, _maxTracerDistance,
                out var endPos);

            SpawnTracer(origin, endPos);

            // 弾道上のポイント粒子を回収できるのはプレイヤーの弾だけ
            if (_attackerId == PlayerConstants.PlayerId)
            {
                _hitscan.PullPointParticles(origin, endPos, BulletData.Size);
            }

            // 手前の敵から順にヒット処理する（貫通順序に依存する効果のため）
            for (var i = 0; i < _hitscan.HitCount; i++)
            {
                if (!CanHit) break;
                HitProcess(_hitscan.GetHitCollider(i));
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

            // ポイント粒子は弾を止めずに通過させる。プレイヤーの弾だけが回収できる
            if (col.TryGetComponent<IPointParticleView>(out var pointParticle))
            {
                if (_attackerId == PlayerConstants.PlayerId)
                {
                    // 撃った瞬間に消さず、プレイヤーへ吸い込まれてから回収される
                    pointParticle.StartPull();
                }

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

            // 同一弾内で何体目のヒットか（1始まり）。ブルズアイの倍率計算に使う。
            // エイム状態とフォーカス対象一致はキリングコールの条件判定に使う
            // isProjectile: 弾の直撃であることを伝える（回避時跳ね返し攻撃の接触弾カウントに使う）
            // projectileId: この弾を一意に識別するId（同じ弾を重複カウントしないために使う）
            hitBox.OnHit(BulletData.Damage, _attackerId, transform.position, out var canPenetrable,
                _hitTargetIds.Count, BulletData.ShotType, BulletData.FocusType, _focusTargetId == hitBox.Id, true,
                ProjectileId);

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

            // フォーカス弾は貫通数に関係なく、フォーカス対象に当たるまで貫通し続ける
            if (BulletData.FocusType == AimFocusType.Focus)
            {
                return;
            }

            // Penetration は「突き抜ける敵の数」。Penetration+1 体目に当たった時点で止める
            if (_hitCount > BulletData.Penetration)
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
                // ポイント粒子はダメージ対象ではないうえ、バッファを埋めて本来の対象を押し出すため除外する
                Physics.AllLayers & ~LayerConstants.PointParticle,
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

            _trailTime *= 0.5f;
            if (!_isOverclockHold)
            {
                _trailRenderer.time = _trailTime;
            }

            // トレイルが縮みきるまで待ってから戻す。オーバークロック中は待機を進めず、軌跡を残したままにする。
            // 待機中に寿命切れや一括消去で先に戻った（＝再貸出され得る）場合は、貸出トークンの取り消しで待機をやめる
            var token = _rentCts != null ? _rentCts.Token : destroyCancellationToken;
            var elapsed = 0f;
            while (elapsed < _trailTime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (!IsTimeHold)
                {
                    elapsed += Time.deltaTime;
                }
            }

            ReturnToPool();
        }
    }
}