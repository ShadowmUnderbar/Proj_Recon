using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// ダメージ・ノードの効果計算。
    /// 有効な依存ノード1種類ごとに Value1 分の攻撃力を加算し、依存ノードを持たない場合は Value2 倍のデメリットを受ける。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public class DamageNodeDataStore : IDamageNodeDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IDependencyNodeDataStore _dependencyNodeDataStore;

        [Inject]
        public DamageNodeDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IDependencyNodeDataStore dependencyNodeDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _dependencyNodeDataStore = dependencyNodeDataStore;
        }

        public float GetDamageMultiplier()
        {
            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.DamageNode, out var upgrade))
            {
                return 1f;
            }

            var nodeCount = _dependencyNodeDataStore.ActiveNodeCount;
            if (nodeCount <= 0)
            {
                return upgrade.Value2.value;
            }

            return 1f + upgrade.Value1.value * nodeCount;
        }
    }
}
