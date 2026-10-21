using App.Battle.Interface.DataStore;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ホップステップ: 回避回数の上限を Value1 回増やし、回避のクールダウンに Value2 を掛ける。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class HopStepsDataStore : IHopStepsDataStore
    {
        private const float DefaultCooldownMultiplier = 1f;

        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public HopStepsDataStore(IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore)
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public int GetExtraDodgeCount()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.HopSteps, out var upgrade))
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(upgrade.Value1.value));
        }

        public float GetCooldownMultiplier()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.HopSteps, out var upgrade))
            {
                return DefaultCooldownMultiplier;
            }

            return Mathf.Max(0f, upgrade.Value2.value);
        }
    }
}
