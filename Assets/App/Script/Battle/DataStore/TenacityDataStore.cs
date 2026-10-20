using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 執念: 被弾すると、受けたダメージ × Value1 を即座に回復する。レイジ発動中なら × Value2。
    /// 受けるダメージ自体は減らさない（レイジの蓄積を妨げないため）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class TenacityDataStore : ITenacityDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IBuffStateDataStore _buffStateDataStore;

        [Inject]
        public TenacityDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IBuffStateDataStore buffStateDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _buffStateDataStore = buffStateDataStore;
        }

        public float GetHealAmount(float damage)
        {
            if (damage <= 0f ||
                !_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Tenacity, out var upgrade))
            {
                return 0f;
            }

            var ratio = _buffStateDataStore.IsRageActive ? upgrade.Value2.value : upgrade.Value1.value;
            return damage * Mathf.Max(0f, ratio);
        }
    }
}
