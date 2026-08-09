using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ケア・ノードの効果計算。
    /// 有効な依存ノード1種類ごとに毎秒 最大HP×Value1 を回復し、依存ノードを持たない場合は
    /// 毎秒 最大HP×Value2（最低 Value3）のダメージを受ける。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class CareNodeDataStore : ICareNodeDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IDependencyNodeDataStore _dependencyNodeDataStore;

        [Inject]
        public CareNodeDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IDependencyNodeDataStore dependencyNodeDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _dependencyNodeDataStore = dependencyNodeDataStore;
        }

        public bool TryGetHealPerSecond(float maxHealth, out float amount)
        {
            amount = 0f;

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.CareNode, out var upgrade))
            {
                return false;
            }

            var nodeCount = _dependencyNodeDataStore.ActiveNodeCount;
            if (nodeCount <= 0)
            {
                return false;
            }

            amount = maxHealth * upgrade.Value1.value * nodeCount;
            return amount > 0f;
        }

        public bool TryGetDamagePerSecond(float maxHealth, out float amount)
        {
            amount = 0f;

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.CareNode, out var upgrade))
            {
                return false;
            }

            if (_dependencyNodeDataStore.ActiveNodeCount > 0)
            {
                return false;
            }

            // 割合ダメージが小さすぎる場合でも最低 Value3 は通す
            amount = Mathf.Max(upgrade.Value3.value, maxHealth * upgrade.Value2.value);
            return amount > 0f;
        }
    }
}
