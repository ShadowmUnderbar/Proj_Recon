using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerStateDataStore : IPlayerStateDataStore, IInitializable
    {
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

            Health.Value = Mathf.Max(0f, Health.Value - damage);

            // 受けたダメージ量を通知（被弾条件バフ用）。HP減少後に発火し、購読側でダメージ量を参照できる
            _onDamaged.OnNext(damage);
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