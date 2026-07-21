using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerStateDataStore : IPlayerStateDataStore, IInitializable
    {
        private readonly IPlayerBarrierDataStore _playerBarrierDataStore;

        [Inject]
        public PlayerStateDataStore(IPlayerBarrierDataStore playerBarrierDataStore)
        {
            _playerBarrierDataStore = playerBarrierDataStore;
        }

        public ReactiveProperty<Vector3> Position { get; } = new();
        public ReactiveProperty<Quaternion> Rotate { get; } = new();
        public Pose Pose => new(Position.Value, Rotate.Value);
        public Transform PlayerTransform { get; set; }

        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();

        private readonly Subject<float> _onDamaged = new();

        /// <summary>被弾したダメージ量を流す（HP減少と同時。被弾条件バフの駆動に使う）</summary>
        public Observable<float> OnDamaged => _onDamaged;

        public float MoveSpeed => BaseSpeed * BasePlayerParameter.MoveSpeed;
        public UnlockCoreSkillType UnlockCoreSkillType { get; private set; } = UnlockCoreSkillType.First;

        private const float BaseSpeed = 0.05f;

        public void Initialize()
        {
            Position.Value = Vector3.zero;
            Health.Value = BasePlayerParameter.Health;
            MaxHealth.Value = BasePlayerParameter.Health;
        }

        public void Move(Vector2 moveV2, float speed)
        {
            Position.Value += new Vector3(moveV2.x, 0, moveV2.y) * speed;
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            // バリアが残っていれば攻撃を全て吸収し、この攻撃ではHPを減らさない（超過分も破棄）。
            // 吸収できなかった（バリア0）ときだけHPを減らす。
            if (!_playerBarrierDataStore.TryAbsorb(damage))
            {
                Health.Value = Mathf.Max(0f, Health.Value - damage);
            }

            // 吸収の有無に関わらず被弾を通知してバリアの回復待機タイマーをリセットする
            _playerBarrierDataStore.NotifyDamaged();

            // 受けたダメージ量を通知（被弾条件バフ用）。バリアで防いだ場合も発火させ、
            // レイジ等の被ダメトリガーは通常どおり駆動させる
            _onDamaged.OnNext(damage);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Health.Value = Mathf.Min(MaxHealth.Value, Health.Value + amount);
        }

        public void SetUnlockCoreSkillType(UnlockCoreSkillType unlockCoreSkillType)
        {
            if (UnlockCoreSkillType >= unlockCoreSkillType)
            {
                return;
            }

            UnlockCoreSkillType = unlockCoreSkillType;
        }
    }
}