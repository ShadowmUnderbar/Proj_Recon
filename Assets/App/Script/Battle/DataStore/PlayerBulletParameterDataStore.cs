using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Interface;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerBulletParameterDataStore : IPlayerBulletParameterDataStore, ITickable
    {
        private readonly IPlayerSettingDataStore _playerSettingDataStore;

        private float _leftShotCoolDown;
        private float _rightShotCoolDown;

        [Inject]
        public PlayerBulletParameterDataStore(
            IPlayerSettingDataStore playerSettingDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
        }

        public void Tick()
        {
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
                ShotType.Merge => BasePlayerParameter.MergeFireRateMagnification,
                ShotType.Waltz => BasePlayerParameter.WaltzFireRateMagnification,
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

        public bool CanShot(HandType handType, ShotType shotType)
        {
            if (_playerSettingDataStore.NonDominantHand.Value == handType &&
                shotType == ShotType.Merge)
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
                ShotType.Merge => BasePlayerParameter.MergeDamageMagnification,
                ShotType.Waltz => BasePlayerParameter.WaltzDamageMagnification,
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
                ShotType.Merge => BasePlayerParameter.MergeBulletSpeedMagnification,
                ShotType.Waltz => BasePlayerParameter.WaltzBulletSpeedMagnification,
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

            if (shotType == ShotType.Merge)
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
    }
}