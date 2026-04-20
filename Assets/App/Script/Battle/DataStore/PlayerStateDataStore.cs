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
        private readonly IPlayerBuffDataStore _playerBuffDataStore;

        public ReactiveProperty<Vector3> Position { get; } = new();
        public ReactiveProperty<Quaternion> Rotate { get; } = new();
        public Pose Pose => new(Position.Value, Rotate.Value);
        public Transform PlayerTransform { get; set; }

        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();

        public float MoveSpeed => BaseSpeed * BasePlayerParameter.MoveSpeed
                                  * _playerBuffDataStore.GetEffectMultiplier(BuffType.Speed);

        public UnlockCoreSkillType UnlockCoreSkillType { get; private set; } = UnlockCoreSkillType.First;

        private const float BaseSpeed = 0.05f;

        [Inject]
        public PlayerStateDataStore(IPlayerBuffDataStore playerBuffDataStore)
        {
            _playerBuffDataStore = playerBuffDataStore;
        }

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