using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// CACHUMMYYY!: キャッシュ（ポイント粒子）を1個回収するたびに体力を回復する。
    /// Value1=回復する粒子の最低値 / Value2=固定の回復量 / Value3=粒子の値に掛ける回復量の割合。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class CachummyyyDataStore : ICachummyyyDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public CachummyyyDataStore(IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore)
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public float GetHealAmount(int point)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Cachummyyy, out var upgrade))
            {
                return 0f;
            }

            if (point < upgrade.Value1.value)
            {
                return 0f;
            }

            return Mathf.Max(0f, upgrade.Value2.value + point * upgrade.Value3.value);
        }
    }
}
