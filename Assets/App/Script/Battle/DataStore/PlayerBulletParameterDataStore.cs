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
        private readonly IBuffStateDataStore _buffStateDataStore;
        private readonly IPeaceMakerDataStore _peaceMakerDataStore;
        private readonly IAvalancheDataStore _avalancheDataStore;
        private readonly IDamageNodeDataStore _damageNodeDataStore;
        private readonly IShotConflictDataStore _shotConflictDataStore;

        // パリィ弾がワルツ／マージのフォーム強化を引き継ぐようになる継承フォーム数
        private const int WaltzInheritLevel = 2;
        private const int MergeInheritLevel = 3;

        private float _leftShotCoolDown;
        private float _rightShotCoolDown;

        [Inject]
        public PlayerBulletParameterDataStore(
            IPlayerSettingDataStore playerSettingDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore,
            IPeaceMakerDataStore peaceMakerDataStore,
            IAvalancheDataStore avalancheDataStore,
            IDamageNodeDataStore damageNodeDataStore,
            IShotConflictDataStore shotConflictDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
            _peaceMakerDataStore = peaceMakerDataStore;
            _avalancheDataStore = avalancheDataStore;
            _damageNodeDataStore = damageNodeDataStore;
            _shotConflictDataStore = shotConflictDataStore;
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

            // バフによる連射速度倍率（倍率が大きいほど連射が速い＝クールダウンを短縮するため除算する）
            coolDown /= _buffStateDataStore.CalcMultiply(BuffEffectType.FireRate);

            coolDown *= focusType == AimFocusType.Focus ? BasePlayerParameter.FocusFireRateMagnification : 1f;

            coolDown *= shotType switch
            {
                ShotType.Merge => BasePlayerParameter.MergeFireRateMagnification,
                ShotType.Waltz => BasePlayerParameter.WaltzFireRateMagnification,
                _ => 1f
            };

            // コンフリクト系（射撃手段の封印と引き換えの連射強化）
            coolDown *= _shotConflictDataStore.GetCoolDownMultiplier();

            // フォーム別の連射補正（ピースメイカー: 連続ノーマルショット / 雪崩: 直前マージの命中）。
            // 倍率取得は状態更新より先に行う（この1発に適用される倍率で確定させる）
            coolDown *= _peaceMakerDataStore.GetCoolDownMultiplier(shotType);
            coolDown *= _avalancheDataStore.GetCoolDownMultiplier(shotType);

            _peaceMakerDataStore.NotifyShot(shotType);
            _avalancheDataStore.NotifyShot(shotType);

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
            // コンフリクト系で封印されたフォームは発射できない
            if (_shotConflictDataStore.IsShotTypeLocked(shotType))
            {
                return false;
            }

            // 二丁拳銃が未解放、またはコンフリクト系で封印されている間は利き手でしか撃てない
            if (DebugConfig.IsVRMode &&
                (!_coreSkillUnlockDataStore.IsUnLockAkimbo || _shotConflictDataStore.IsAkimboLocked) &&
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
            var damage = GetBulletDamage(shotType, focusType);

            var bullet = new BulletData
            {
                ShotType = shotType,
                FocusType = focusType,
                Speed = shotType == ShotType.Merge ? BasePlayerParameter.MergeBulletSpeed : 0, //プレイヤーは即着弾
                Damage = damage,
                Penetration = GetBulletPenetration(shotType, focusType),
                Explosive = GetBulletExplosive(shotType, focusType),
                ExplosiveDamage = GetBulletExplosiveDamage(shotType, damage)
            };

            // 非フォーカス時のみ、弾サイズ（＝当たり判定サイズ）に HitRange 強化を乗算する。
            // フォーカス弾には影響しない（BulletData.Size は BaseBulletView で見た目スケールと SphereCast 判定の両方に使われる）
            if (focusType == AimFocusType.NotFocus)
            {
                bullet.Size *= _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.HitRange);
            }

            return bullet;
        }

        public BulletData GetParryBulletData(int inheritedFormCount)
        {
            // 基礎性能は「撃ってきた相手へのフォーカスショット」。
            // ノーマル弾のフォーカス射撃なので、この時点でノーマル(Lv1)分の強化は含まれている
            var bullet = GetBulletData(ShotType.Normal, AimFocusType.Focus);

            if (inheritedFormCount >= WaltzInheritLevel)
            {
                bullet.Damage *= _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.WaltzDamage);
            }

            if (inheritedFormCount >= MergeInheritLevel)
            {
                bullet.Damage *= _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.MergeDamage);
            }

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

            // ダメージ・ノード（有効な依存ノードの種類数で倍率が変動。未所持なら1倍）
            damage *= _damageNodeDataStore.GetDamageMultiplier();

            // フォーム別ダメージアップ（Normal/Waltz/Merge それぞれ該当フォームの弾にのみ乗算）
            damage *= shotType switch
            {
                ShotType.Merge => _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.MergeDamage),
                ShotType.Waltz => _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.WaltzDamage),
                _ => _upgradeEffectSimpleCalculatorDataStore.CalcMultiply(UpgradeType.NormalDamage)
            };

            // バフによる攻撃力倍率（爆風ダメージは弾ダメージから算出するため爆風にも効く）
            damage *= _buffStateDataStore.CalcMultiply(BuffEffectType.AttackPower);

            damage *= focusType == AimFocusType.Focus ? BasePlayerParameter.LongFocusDamageMagnification : 1f;

            // コンフリクト系（射撃手段の封印と引き換えのダメージ強化）
            damage *= _shotConflictDataStore.GetDamageMultiplier();

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

        // 爆風ダメージは弾ダメージに割合を乗じて算出する。
        // 弾ダメージ側の強化・バフ・フォーカス倍率がそのまま爆風にも反映される
        private float GetBulletExplosiveDamage(ShotType shotType, float bulletDamage)
        {
            if (shotType != ShotType.Merge)
            {
                return 0f;
            }

            return bulletDamage * BasePlayerParameter.MergeExplosiveDamageRate;
        }
    }
}