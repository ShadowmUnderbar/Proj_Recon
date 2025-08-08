using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDataStore : IPlayerDataStore, IInitializable, ITickable
    {
        private readonly IEnemyDataStore _enemyDataStore;

        public ReactiveProperty<Vector3> Position { get; } = new();

        public ReactiveProperty<Quaternion> Rotate { get; } = new();
        public Pose Pose => new Pose(Position.Value, Rotate.Value);
        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();

        public ReactiveProperty<ShotType> ShotType { get; } = new();
        public ReactiveProperty<AimFocusType> LeftFocusType { get; } = new();
        public ReactiveProperty<AimFocusType> RightFocusType { get; } = new();

        public ReactiveProperty<int> FocusLeftTargetId { get; } = new(-1);
        public ReactiveProperty<int> FocusRightTargetId { get; } = new(-1);
        public ReactiveProperty<Pose> LeftHandPose { get; } = new();
        public ReactiveProperty<Pose> RightHandPose { get; } = new();
        
        public Vector3 LeftAimDirection => (Position.Value - _aimPositions[HandType.Left]).normalized;
        public Vector3 RightAimDirection => (Position.Value - _aimPositions[HandType.Right]).normalized;

        public float MoveSpeed => BaseSpeed * BasePlayerParameter.MoveSpeed;

        private const float BaseSpeed = 0.05f;

        private static float MergePositionDistance => 0.15f;
        private static float WaltzAngleDifference => 130f;
        private static float LongFocusDistance => 17f;

        private readonly Dictionary<HandType, Vector3> _aimPositions = new()
        {
            { HandType.Left, Vector3.zero },
            { HandType.Right, Vector3.zero }
        };

        [Inject]
        public PlayerDataStore(
            IEnemyDataStore enemyDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
        }

        public void Initialize()
        {
            Position.Value = Vector3.zero;
            Health.Value = BasePlayerParameter.Health;
            MaxHealth.Value = BasePlayerParameter.Health;

            ShotType.Value = Common.Data.ShotType.Normal;
            LeftFocusType.Value = AimFocusType.NotFocus;
            RightFocusType.Value = AimFocusType.NotFocus;

            FocusLeftTargetId.Value = -1;
            FocusRightTargetId.Value = -1;
        }

        public void Tick()
        {
            UpdateShotType();
            UpdateLeftFocusType();
            UpdateRightFocusType();
        }

        private void UpdateShotType()
        {
            if (IsMerge())
            {
                ShotType.Value = Common.Data.ShotType.Merge;
                return;
            }

            if (IsWaltz())
            {
                ShotType.Value = Common.Data.ShotType.Waltz;
                return;
            }

            ShotType.Value = Common.Data.ShotType.Normal;
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

            var distance = (enemy.Pose.position - Position.Value).sqrMagnitude;
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

            var distance = (enemy.Pose.position - Position.Value).sqrMagnitude;
            if (Mathf.Abs(distance) >= LongFocusDistance * LongFocusDistance)
            {
                RightFocusType.Value = AimFocusType.LongFocus;
                return;
            }

            RightFocusType.Value = AimFocusType.Focus;
        }

        private bool IsMerge()
        {
            var distance = (LeftHandPose.Value.position - RightHandPose.Value.position).sqrMagnitude;
            return Mathf.Abs(distance) < MergePositionDistance * MergePositionDistance;
        }

        private bool IsWaltz()
        {
            var leftAimDirection = (Position.Value - _aimPositions[HandType.Left]).normalized;
            var rightAimDirection = (Position.Value - _aimPositions[HandType.Right]).normalized;

            var angleDifference = Vector3.SignedAngle(leftAimDirection, rightAimDirection, Vector3.up);
            return Mathf.Abs(angleDifference) >= WaltzAngleDifference;
        }

        public void SetAimPosition(HandType handType, Vector3 position)
        {
            _aimPositions[handType] = position;
        }

        public void Move(Vector2 moveV2, float speed)
        {
            Position.Value += new Vector3(moveV2.x, 0, moveV2.y) * speed;
        }
    }
}