using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ヴァンパイア: 敵を撃破するたびに体力を Value1 回復する（敵のランクを問わない）。
    /// レベルは累積せず、所持中の最高レベルのみを採用する
    /// </summary>
    public class VampireDataStore : IEnemyKillHook
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public VampireDataStore(IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore)
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        /// <summary>ランクを問わず Value1。未所持なら 0</summary>
        public float GetHealAmount(EnemyRankType rankType)
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.Vampire, out var upgrade))
            {
                return 0f;
            }

            return Mathf.Max(0f, upgrade.Value1.value);
        }
    }
}
