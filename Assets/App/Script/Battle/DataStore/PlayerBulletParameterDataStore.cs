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
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        private float _leftShotCoolDown;
        private float _rightShotCoolDown;

        [Inject]
        public PlayerBulletParameterDataStore(
            IPlayerSettingDataStore playerSettingDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public void Tick()
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

        public void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType)
        {
            var coolDown = BasePlayerParameter.BaseFireRate;

            coolDown *= _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.FireRate);

            coolDown *= focusType == AimFocusType.Focus ? BasePlayerParameter.FocusFireRateMagnification : 1f;

            coolDown *= shotType switch
            {
                ShotType.Merge => BasePlayerParameter.MergeFireRateMagnification,
                ShotType.Waltz => BasePlayerParameter.WaltzFireRateMagnification,
                _ => 1f
            };

            if (handType == HandType.Left)
            {
                _leftShotCoolDown = coolDown;
            }
            else
            {
                _rightShotCoolDown = coolDown;
            }
        }

        public bool CanShot(HandType handType, ShotType shotType)
        {
            if (DebugConfig.IsVRMode &&
                !_coreSkillUnlockDataStore.IsUnLockAkimbo &&
                _playerSettingDataStore.NonDominantHand == handType)
            {
                return false;
            }

            if (shotType == ShotType.Waltz &&
                !_coreSkillUnlockDataStore.IsUnLockWaltz)
            {
                return false;
            }

            if (shotType == ShotType.Merge &&
                !CanMergeShot(handType))
            {
                return false;
            }

            if (handType == HandType.Left)
            {
                return _leftShotCoolDown <= 0;
            }

            return _rightShotCoolDown <= 0;
        }

        private bool CanMergeShot(HandType handType)
        {
            if (!_coreSkillUnlockDataStore.IsUnLockMerge)
            {
                return false;
            }

            if (_playerSettingDataStore.DominantHand.Value != handType)
            {
                return false;
            }

            return true;
        }

        public BulletData GetBulletData(ShotType shotType, AimFocusType focusType)
        {
            var bullet = new BulletData
            {
                ShotType = shotType,
                FocusType = focusType,
                Speed = shotType == ShotType.Merge ? BasePlayerParameter.MergeBulletSpeed : 0, //プレイヤーは即着弾
                Damage = GetBulletDamage(shotType, focusType),
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

            damage *= _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.BulletDamage);
            damage *= focusType == AimFocusType.Focus ? BasePlayerParameter.LongFocusDamageMagnification : 1f;

            return damage;
        }

        private int GetBulletPenetration(ShotType shotType, AimFocusType focusType)
        {
            var penetration = (float)BasePlayerParameter.BasePenetration;
            return Mathf.CeilToInt(penetration);
        }

        private float GetBulletExplosive(ShotType shotType, AimFocusType focusType)
        {
            var explosive = 0f;

            if (shotType == ShotType.Merge)
            {
                explosive += BasePlayerParameter.MergeExplosiveScale;
            }

            explosive *= _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.BombRange);
            return explosive;
        }
    }
}