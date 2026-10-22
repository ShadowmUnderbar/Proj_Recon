using App.Battle.Data;
using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// アンチマテリアル: 射撃（プレイヤー・味方の弾）の命中の最低保証ダメージを Value1 に引き上げる。
    /// 弱点・耐性方向の倍率の後で効く（適用は EnemyDataStore.Damage）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class AntiMaterialDataStore : IHitEffectHook
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public AntiMaterialDataStore(IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore)
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public int Order => HitHookOrder.AntiMaterial;

        /// <summary>射撃（ShotType あり）の命中だけ最低保証ダメージを引き上げる。適用は EnemyDataStore.Damage</summary>
        public void OnBeforeDamage(HitData hitData)
        {
            if (hitData.ShotType == null)
            {
                return;
            }

            hitData.MinDamage = GetMinDamage();
        }

        /// <summary>所持中の最高レベルの Value1。未所持、または既定値より小さいときは <see cref="HitData.DefaultMinDamage"/></summary>
        private float GetMinDamage()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.AntiMaterial, out var upgrade))
            {
                return HitData.DefaultMinDamage;
            }

            return Mathf.Max(HitData.DefaultMinDamage, upgrade.Value1.value);
        }
    }
}
