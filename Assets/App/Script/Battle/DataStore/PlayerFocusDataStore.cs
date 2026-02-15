using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerFocusDataStore : IPlayerFocusDataStore, IInitializable, ITickable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        public ReactiveProperty<int> FocusLeftTargetId { get; } = new(-1);
        public ReactiveProperty<int> FocusRightTargetId { get; } = new(-1);
        public ReactiveProperty<AimFocusType> LeftFocusType { get; } = new();
        public ReactiveProperty<AimFocusType> RightFocusType { get; } = new();

        public bool IsFocusInput { get; set; }
        public ReactiveProperty<bool> IsLeftFocusInput { get; } = new();
        public ReactiveProperty<bool> IsRightFocusInput { get; } = new();

        private const float LongFocusDistance = 17f;

        [Inject]
        public PlayerFocusDataStore(
            IPlayerStateDataStore playerStateDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _enemyDataStore = enemyDataStore;
        }

        public void Initialize()
        {
            LeftFocusType.Value = AimFocusType.NotFocus;
            RightFocusType.Value = AimFocusType.NotFocus;
            FocusLeftTargetId.Value = -1;
            FocusRightTargetId.Value = -1;
        }

        public void Tick()
        {
            UpdateLeftFocusType();
            UpdateRightFocusType();
        }

        private void UpdateLeftFocusType()
        {
            if (FocusLeftTargetId.Value == -1)
            {
                LeftFocusType.Value = AimFocusType.NotFocus;
                return;
            }

            if (!_enemyDataStore.TryGetEnemyData(FocusLeftTargetId.Value, out var enemy))
            {
                LeftFocusType.Value = AimFocusType.NotFocus;
                return;
            }

            var distance = (enemy.Pose.position - _playerStateDataStore.Position.Value).sqrMagnitude;
            if (Mathf.Abs(distance) >= LongFocusDistance * LongFocusDistance)
            {
                LeftFocusType.Value = AimFocusType.LongFocus;
                return;
            }

            LeftFocusType.Value = AimFocusType.Focus;
        }

        private void UpdateRightFocusType()
        {
            if (FocusRightTargetId.Value == -1)
            {
                RightFocusType.Value = AimFocusType.NotFocus;
                return;
            }

            if (!_enemyDataStore.TryGetEnemyData(FocusRightTargetId.Value, out var enemy))
            {
                RightFocusType.Value = AimFocusType.NotFocus;
                return;
            }

            var distance = (enemy.Pose.position - _playerStateDataStore.Position.Value).sqrMagnitude;
            if (Mathf.Abs(distance) >= LongFocusDistance * LongFocusDistance)
            {
                RightFocusType.Value = AimFocusType.LongFocus;
                return;
            }

            RightFocusType.Value = AimFocusType.Focus;
        }
    }
}
