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
            UpdateFocusType(FocusLeftTargetId, LeftFocusType);
            UpdateFocusType(FocusRightTargetId, RightFocusType);
        }

        private void UpdateFocusType(ReactiveProperty<int> focusTargetId, ReactiveProperty<AimFocusType> focusType)
        {
            if (focusTargetId.Value == -1)
            {
                focusType.Value = AimFocusType.NotFocus;
                return;
            }

            if (!_enemyDataStore.TryGetEnemyData(focusTargetId.Value, out var enemy))
            {
                focusType.Value = AimFocusType.NotFocus;
                return;
            }

            var distance = (enemy.Pose.position - _playerStateDataStore.Position.Value).sqrMagnitude;
            if (Mathf.Abs(distance) >= LongFocusDistance * LongFocusDistance)
            {
                focusType.Value = AimFocusType.LongFocus;
                return;
            }

            focusType.Value = AimFocusType.Focus;
        }
    }
}
