using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    public class PlayerAimDataStore : IPlayerAimDataStore
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IPlayerFocusDataStore _playerFocusDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        public ReactiveProperty<Pose> LeftHandPose { get; } = new();
        public ReactiveProperty<Pose> RightHandPose { get; } = new();

        public Vector3 LeftAimDirection =>
            (_playerStateDataStore.Position.Value - _aimPositions[HandType.Left]).normalized;

        public Vector3 RightAimDirection =>
            (_playerStateDataStore.Position.Value - _aimPositions[HandType.Right]).normalized;

        private readonly Dictionary<HandType, Vector3> _aimPositions = new()
        {
            { HandType.Left, Vector3.zero },
            { HandType.Right, Vector3.zero }
        };

        [Inject]
        public PlayerAimDataStore(
            IPlayerStateDataStore playerStateDataStore,
            IPlayerFocusDataStore playerFocusDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _playerFocusDataStore = playerFocusDataStore;
            _enemyDataStore = enemyDataStore;
        }

        public void SetAimPosition(HandType handType, Vector3 position)
        {
            if (handType == HandType.Left && _playerFocusDataStore.FocusLeftTargetId.Value != -1)
            {
                if (_enemyDataStore.TryGetEnemyData(_playerFocusDataStore.FocusLeftTargetId.Value, out var enemy))
                {
                    _aimPositions[handType] = enemy.Pose.position;
                    return;
                }
            }
            else if (handType == HandType.Right && _playerFocusDataStore.FocusRightTargetId.Value != -1)
            {
                if (_enemyDataStore.TryGetEnemyData(_playerFocusDataStore.FocusRightTargetId.Value, out var enemy))
                {
                    _aimPositions[handType] = enemy.Pose.position;
                    return;
                }
            }

            _aimPositions[handType] = position;
        }
    }
}
