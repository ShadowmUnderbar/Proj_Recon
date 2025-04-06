using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDataStore : IPlayerDataStore, ITickable
    {
        public ReactiveProperty<Vector3> Position { get; } = new();
        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();

        public ReactiveProperty<ShotType> ShotType { get; } = new();
        public ReactiveProperty<int> FocusLeftTargetId { get; } = new();
        public ReactiveProperty<int> FocusRightTargetId { get; } = new();

        public float NormalFireRate => 0.6f;
        public float MergeFireRate => 1.2f;
        public float WaltzFireRate => 0.3f;

        private float _leftNormalShotCoolDown;
        private float _rightNormalShotCoolDown;
        private float _mergeShotCoolDown;
        private float _leftWaltzShotCoolDown;
        private float _rightWaltzShotCoolDown;

        private float WaltzAngleDifference => 130f;

        private readonly Dictionary<bool, Vector3> _aimPositions = new()
        {
            { false, Vector3.zero },
            { true, Vector3.zero }
        };

        public void Tick()
        {
            UpdateShotType();
            UpdateCoolDownTime();
        }

        private void UpdateShotType()
        {
            if (FocusLeftTargetId.Value != -1 &&
                FocusRightTargetId.Value != -1 &&
                FocusLeftTargetId.Value == FocusRightTargetId.Value)
            {
                ShotType.Value = Common.Data.ShotType.Merge;
                return;
            }

            var leftAimDirection = (Position.Value - _aimPositions[false]).normalized;
            var rightAimDirection = (Position.Value - _aimPositions[true]).normalized;

            var angleDifference = Vector3.SignedAngle(leftAimDirection, rightAimDirection, Vector3.up);

            if (Mathf.Abs(angleDifference) >= WaltzAngleDifference)
            {
                ShotType.Value = Common.Data.ShotType.Waltz;
                return;
            }

            ShotType.Value = Common.Data.ShotType.Normal;
        }

        private void UpdateCoolDownTime()
        {
            if (_leftNormalShotCoolDown > 0)
            {
                _leftNormalShotCoolDown -= Time.deltaTime;
            }

            if (_rightNormalShotCoolDown > 0)
            {
                _rightNormalShotCoolDown -= Time.deltaTime;
            }

            if (_mergeShotCoolDown > 0)
            {
                _mergeShotCoolDown -= Time.deltaTime;
            }

            if (_leftWaltzShotCoolDown > 0)
            {
                _leftWaltzShotCoolDown -= Time.deltaTime;
            }

            if (_rightWaltzShotCoolDown > 0)
            {
                _rightWaltzShotCoolDown -= Time.deltaTime;
            }
        }

        public bool CanShotCoolDown(bool isLeft, ShotType shotType)
        {
            switch (shotType)
            {
                case Common.Data.ShotType.Normal:
                    return isLeft ? CanLeftNormalShot : CanRightNormalShot;
                case Common.Data.ShotType.Merge:
                    return CanMergeShot;
                case Common.Data.ShotType.Waltz:
                    return isLeft ? CanLeftWaltzShot : CanRightWaltzShot;
                default:
                    return false;
            }
        }

        private bool CanLeftNormalShot => _leftNormalShotCoolDown <= 0;
        private bool CanRightNormalShot => _rightNormalShotCoolDown <= 0;
        private bool CanMergeShot => _mergeShotCoolDown <= 0;
        private bool CanLeftWaltzShot => _leftWaltzShotCoolDown <= 0;
        private bool CanRightWaltzShot => _rightWaltzShotCoolDown <= 0;

        public void SetLeftNormalShotCoolDown(float time)
        {
            _leftNormalShotCoolDown = time;
        }

        public void SetRightNormalShotCoolDown(float time)
        {
            _rightNormalShotCoolDown = time;
        }

        public void SetMergeShotCoolDown(float time)
        {
            _mergeShotCoolDown = time;
        }

        public void SetLeftWaltzShotCoolDown(float time)
        {
            _leftWaltzShotCoolDown = time;
        }

        public void SetRightWaltzShotCoolDown(float time)
        {
            _rightWaltzShotCoolDown = time;
        }

        public void SetAimPosition(bool isLeft, Vector3 position)
        {
            _aimPositions[isLeft] = position;
        }
    }
}