using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDataStore : IPlayerDataStore, IInitializable, ITickable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerSettingDataStore _playerSettingDataStore;

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
            IEnemyDataStore enemyDataStore,
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
            _playerSettingDataStore = playerSettingDataStore;
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
            UpdateCoolDownTime();
        }

        public void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType)
        {
            var baseCoolDown = BasePlayerParameter.BaseFireRate;

            baseCoolDown *= focusType switch
            {
                AimFocusType.Focus => BasePlayerParameter.FocusFireRateMagnification,
                AimFocusType.LongFocus => BasePlayerParameter.LongFocusFireRateMagnification,
                _ => 1f
            };

            baseCoolDown *= shotType switch
            {
                Common.Data.ShotType.Merge => BasePlayerParameter.MergeFireRateMagnification,
                Common.Data.ShotType.Waltz => BasePlayerParameter.WaltzFireRateMagnification,
                _ => 1f
            };

            if (handType == HandType.Left)
            {
                _leftShotCoolDown = baseCoolDown;
            }
            else
            {
                _rightShotCoolDown = baseCoolDown;
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

        public bool CanShot(HandType handType)
        {
            if (_playerSettingDataStore.NonDominantHand.Value == handType &&
                ShotType.Value == Common.Data.ShotType.Merge)
            {
                return false;
            }

            if (handType == HandType.Left)
            {
                if (_leftShotCoolDown <= 0)
                {
                    return true;
                }

                return false;
            }

            if (_rightShotCoolDown <= 0)
            {
                return true;
            }

            return false;
        }

        public BulletData GetBulletData(ShotType shotType, AimFocusType focusType)
        {
            var bullet = new BulletData
            {
                ShotType = shotType,
                FocusType = focusType,
                Damage = GetBulletDamage(shotType, focusType),
                Speed = GetBulletSpeed(shotType, focusType),
                Penetration = GetBulletPenetration(shotType, focusType),
                Explosive = GetBulletExplosive(shotType, focusType)
            };

            return bullet;
        }

        private float GetBulletDamage(ShotType shotType, AimFocusType focusType)
        {
            var damage = BasePlayerParameter.BaseDamage;
            damage *= shotType switch
            {
                Common.Data.ShotType.Merge => BasePlayerParameter.MergeDamageMagnification,
                Common.Data.ShotType.Waltz => BasePlayerParameter.WaltzDamageMagnification,
                _ => 1f
            };

            damage *= focusType switch
            {
                AimFocusType.Focus => BasePlayerParameter.FocusDamageMagnification,
                AimFocusType.LongFocus => BasePlayerParameter.LongFocusDamageMagnification,
                _ => 1f
            };
            return damage;
        }

        private float GetBulletSpeed(ShotType shotType, AimFocusType focusType)
        {
            var damage = BasePlayerParameter.BaseBulletSpeed;
            damage *= shotType switch
            {
                Common.Data.ShotType.Merge => BasePlayerParameter.MergeBulletSpeedMagnification,
                Common.Data.ShotType.Waltz => BasePlayerParameter.WaltzBulletSpeedMagnification,
                _ => 1f
            };

            damage *= focusType switch
            {
                AimFocusType.Focus => BasePlayerParameter.FocusBulletSpeedMagnification,
                AimFocusType.LongFocus => BasePlayerParameter.LongFocusBulletSpeedMagnification,
                _ => 1f
            };
            return damage;
        }

        private int GetBulletPenetration(ShotType shotType, AimFocusType focusType)
        {
            var penetration = (float)BasePlayerParameter.BasePenetration;

            penetration *= focusType switch
            {
                AimFocusType.Focus => BasePlayerParameter.FocusPenetration,
                AimFocusType.LongFocus => BasePlayerParameter.LongFocusPenetrationMagnification,
                _ => 1
            };
            return Mathf.CeilToInt(penetration);
        }

        private float GetBulletExplosive(ShotType shotType, AimFocusType focusType)
        {
            var explosive = 0f;

            if (shotType == Common.Data.ShotType.Merge)
            {
                explosive += BasePlayerParameter.MergeExplosiveScale;
            }

            explosive *= focusType switch
            {
                AimFocusType.Focus => BasePlayerParameter.FocusExplosiveMagnification,
                AimFocusType.LongFocus => BasePlayerParameter.LongFocusExplosiveMagnification,
                _ => 0
            };
            return explosive;
        }
    }
}