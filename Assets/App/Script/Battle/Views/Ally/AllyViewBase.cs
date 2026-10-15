using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using App.Framework.Utilities;
using R3;
using UnityEngine;

namespace App.Battle.Views.Ally
{
    /// <summary>
    /// 召喚した味方1体の基底。被弾の受け取りと、弾を撃つ手順（要求 → 弾データを受け取って発射）を持つ。
    /// どの敵をいつ狙うかは派生クラスが決める
    /// </summary>
    public abstract class AllyViewBase : MonoBehaviour
    {
        [SerializeField, Tooltip("被弾を受けるコライダー。狙われない味方は無効にする")]
        private AllyDamageReceiverView[] _damageReceivers = System.Array.Empty<AllyDamageReceiverView>();

        // 弾の「フォーカス対象なし」を表すId（PlayerShotView と同じ）
        private const int NoFocusTargetId = -1;

        private readonly Subject<Unit> _onShotRequested = new();
        private readonly Subject<float> _onDamaged = new();

        private ISimpleObjectFactory<IBulletView> _bulletFactory;

        /// <summary>攻撃しようとしている。弾データは <see cref="Shot"/> で受け取る</summary>
        public Observable<Unit> OnShotRequested => _onShotRequested;

        /// <summary>敵の攻撃を受けた（ダメージ量）</summary>
        public Observable<float> OnDamaged => _onDamaged;

        public int Id { get; private set; }

        protected AllyData AllyData { get; private set; }

        protected bool IsPause { get; private set; }

        public virtual void Init(AllyData allyData, ISimpleObjectFactory<IBulletView> bulletFactory)
        {
            Id = allyData.Id;
            AllyData = allyData;
            _bulletFactory = bulletFactory;

            foreach (var receiver in _damageReceivers)
            {
                if (receiver == null)
                {
                    continue;
                }

                receiver.Init(allyData.Id, HitBoxType.Ally, HitDirectionType.None);

                // 狙われない味方は敵の攻撃も当たらない（弾をすり抜ける）
                if (!allyData.IsTargetable && receiver.TryGetComponent<Collider>(out var receiverCollider))
                {
                    receiverCollider.enabled = false;
                }

                receiver.OnDamaged
                    .Subscribe(_onDamaged.OnNext)
                    .AddTo(this);
            }
        }

        public void SetPause(bool isPause)
        {
            IsPause = isPause;
        }

        /// <summary>要求に応じて渡された弾データで撃つ</summary>
        public abstract void Shot(BulletData bulletData);

        protected void RequestShot()
        {
            _onShotRequested.OnNext(Unit.Default);
        }

        /// <summary>指定の位置・向きからプレイヤーの弾を撃つ。撃ち手は自分の Id にする</summary>
        protected void FireBullet(Pose pose, BulletData bulletData)
        {
            if (_bulletFactory == null)
            {
                return;
            }

            var bullet = _bulletFactory.Instantiate(null);
            bullet.Spawn(Id, pose, bulletData, NoFocusTargetId);
        }

        protected virtual void OnDestroy()
        {
            _onShotRequested.Dispose();
            _onDamaged.Dispose();
        }
    }
}
