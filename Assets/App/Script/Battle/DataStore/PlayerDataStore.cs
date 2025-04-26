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
    public class PlayerDataStore : IPlayerDataStore, ITickable
    {
        private readonly BulletDataBase _bulletDataBase;

        public ReactiveProperty<Vector3> Position { get; } = new();
        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();

        public ReactiveProperty<ShotType> ShotType { get; } = new();
        public ReactiveProperty<AimFocusType> FocusType { get; } = new();
        public ReactiveProperty<int> FocusLeftTargetId { get; } = new();
        public ReactiveProperty<int> FocusRightTargetId { get; } = new();

        private float _leftShotCoolDown;
        private float _rightShotCoolDown;

        private float MergePositionDistance => 0.5f;
        private float WaltzAngleDifference => 130f;

        public bool CanLeftShot => _leftShotCoolDown <= 0;
        public bool CanRightShot => _rightShotCoolDown <= 0;

        private readonly Dictionary<HandType, Vector3> _aimPositions = new()
        {
            { HandType.Left, Vector3.zero },
            { HandType.Right, Vector3.zero }
        };

        [Inject]
        public PlayerDataStore(BulletDataBase bulletDataBase)
        {
            _bulletDataBase = bulletDataBase;
        }

        public void Tick()
        {
            UpdateShotType();
            UpdateCoolDownTime();
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

        private bool IsMerge()
        {
            var distance = (_aimPositions[HandType.Left] - _aimPositions[HandType.Right]).sqrMagnitude;
            return distance < MergePositionDistance * MergePositionDistance;
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