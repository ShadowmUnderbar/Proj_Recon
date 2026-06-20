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

        // 各手のエイム対象ワールド座標（フォーカス敵位置 or マウス/手の位置）
        public Vector3 LeftAimPosition => _aimPositions[HandType.Left];
        public Vector3 RightAimPosition => _aimPositions[HandType.Right];

        // 両手のエイム方向（プレイヤー→エイム、XZ平面に投影・正規化）の平均。
        // 両手がほぼ正反対で合成が不安定なときはVector3.zeroを返し、振り向き先を更新させない。
        public Vector3 CenterAimDirection
        {
            get
            {
                var position = _playerStateDataStore.Position.Value;
                var toLeft = Vector3.ProjectOnPlane(_aimPositions[HandType.Left] - position, Vector3.up);
                var toRight = Vector3.ProjectOnPlane(_aimPositions[HandType.Right] - position, Vector3.up);

                var sum = toLeft.normalized + toRight.normalized;
                return sum.sqrMagnitude < CenterAimEpsilon ? Vector3.zero : sum.normalized;
            }
        }

        // 合成ベクトルがこの値より小さい場合は両手が正反対付近とみなす
        private const float CenterAimEpsilon = 0.01f;

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
