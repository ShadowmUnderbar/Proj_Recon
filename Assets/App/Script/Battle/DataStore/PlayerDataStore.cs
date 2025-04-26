using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDataStore : IPlayerDataStore, IInitializable, ITickable
    {
        private readonly BulletDataBase _bulletDataBase;
        private readonly IEnemyDataStore _enemyDataStore;

        public ReactiveProperty<Vector3> Position { get; } = new();
        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();

        public ReactiveProperty<ShotType> ShotType { get; } = new();
        public ReactiveProperty<AimFocusType> LeftFocusType { get; } = new();
        public ReactiveProperty<AimFocusType> RightFocusType { get; } = new();

        public ReactiveProperty<int> FocusLeftTargetId { get; } = new(-1);
        public ReactiveProperty<int> FocusRightTargetId { get; } = new(-1);
        public ReactiveProperty<Pose> LeftHandPose { get; } = new();
        public ReactiveProperty<Pose> RightHandPose { get; } = new();

        private float _leftShotCoolDown;
        private float _rightShotCoolDown;

        private float MergePositionDistance => 0.5f;
        private float WaltzAngleDifference => 130f;

        public bool CanLeftShot => _leftShotCoolDown <= 0;
        public bool CanRightShot => _rightShotCoolDown <= 0;

        private float LongFocusDistance => 8f;

        private readonly Dictionary<HandType, Vector3> _aimPositions = new()
        {
            { HandType.Left, Vector3.zero },
            { HandType.Right, Vector3.zero }
        };

        [Inject]
        public PlayerDataStore(
            BulletDataBase bulletDataBase,
            IEnemyDataStore enemyDataStore
        )
        {
            _bulletDataBase = bulletDataBase;
            _enemyDataStore = enemyDataStore;
        }

        public void Initialize()
        {
            Position.Value = Vector3.zero;
            Health.Value = 100f;
            MaxHealth.Value = 100f;

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

        public void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType)
        {
            if (_bulletDataBase.TryGetBulletData(shotType, focusType, out var bulletData))
            {
                return;
            }

            if (handType == HandType.Left)
            {
                _leftShotCoolDown = bulletData.CoolDownSecound;
            }
            else
            {
                _rightShotCoolDown = bulletData.CoolDownSecound;
            }
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

            if ((enemy.Pose.position - Position.Value).sqrMagnitude > LongFocusDistance)
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

            if ((enemy.Pose.position - Position.Value).sqrMagnitude > LongFocusDistance)
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

        private void UpdateCoolDownTime()
        {
            if (_leftShotCoolDown > 0)
            {
                _leftShotCoolDown -= Time.deltaTime;
            }

            if (_rightShotCoolDown > 0)
            {
                _rightShotCoolDown -= Time.deltaTime;
            }
        }

        public void SetAimPosition(HandType handType, Vector3 position)
        {
            _aimPositions[handType] = position;
        }
    }
}