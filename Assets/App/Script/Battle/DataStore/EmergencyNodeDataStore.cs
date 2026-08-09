using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// エマージェンシー・ノードの効果判定。
    /// 致死ダメージを受けた際、有効な依存ノードを1つ（α→β→γの順）無効化することを代償に、
    /// そのダメージを無効化して最大HP×Value1 まで回復する。
    /// 有効な依存ノードが残っていなければ発動しない（＝そのまま死亡する）。
    /// </summary>
    public class EmergencyNodeDataStore : IEmergencyNodeDataStore
    {
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;
        private readonly IDependencyNodeDataStore _dependencyNodeDataStore;

        [Inject]
        public EmergencyNodeDataStore(
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore,
            IDependencyNodeDataStore dependencyNodeDataStore
        )
        {
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
            _dependencyNodeDataStore = dependencyNodeDataStore;
        }

        public bool TryActivate(out float healRatio)
        {
            healRatio = 0f;

            if (!_upgradeEffectSimpleCalculatorDataStore
                    .TryGetHighestLevelUpgrade(UpgradeType.EmergencyNode, out var upgrade))
            {
                return false;
            }

            // 代償にできる依存ノードが無ければ発動しない
            if (!_dependencyNodeDataStore.TryDisableOne(out var disabledNameKey))
            {
                return false;
            }

            healRatio = upgrade.Value1.value;
            Debug.Log($"[EmergencyNodeDataStore] 致死ダメージを無効化し、依存ノード \"{disabledNameKey}\" を無効化しました");
            return true;
        }
    }
}
