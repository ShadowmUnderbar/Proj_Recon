using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeEffectSimpleCalculatorDataStore : IUpgradeEffectSimpleCalculatorDataStore
    {
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly IPassiveConditionDataStore _passiveConditionDataStore;

        [Inject]
        public UpgradeEffectSimpleCalculatorDataStore(
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeDatabase upgradeDatabase,
            IPassiveConditionDataStore passiveConditionDataStore
        )
        {
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeDatabase = upgradeDatabase;
            _passiveConditionDataStore = passiveConditionDataStore;
        }

        /// <summary>
        /// 指定タイプの全アップグレード Value1 を乗算合成して返す。
        /// 効果なし時は 1.0f。
        /// </summary>
        public float CalcMultiply(UpgradeType upgradeType)
        {
            var result = 1f;
            foreach (var id in _upgradeSessionDataStore.AppliedUpgrades)
            {
                if (!_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                {
                    continue;
                }

                if (data.UpgradeType != upgradeType)
                {
                    continue;
                }

                // パッシブ条件が成立していない場合は寄与させない（None は常に成立）
                if (!_passiveConditionDataStore.IsSatisfied(data.ConditionType, data.ConditionValue))
                {
                    continue;
                }

                result *= data.Value1.value;
            }

            return result;
        }

        /// <summary>
        /// 指定タイプの全アップグレード Value1 を加算して返す。
        /// 効果なし時は 0f。
        /// </summary>
        public float CalcAdd(UpgradeType upgradeType)
        {
            var result = 0f;
            foreach (var id in _upgradeSessionDataStore.AppliedUpgrades)
            {
                if (!_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                {
                    continue;
                }

                if (data.UpgradeType != upgradeType)
                {
                    continue;
                }

                // パッシブ条件が成立していない場合は寄与させない（None は常に成立）
                if (!_passiveConditionDataStore.IsSatisfied(data.ConditionType, data.ConditionValue))
                {
                    continue;
                }

                result += data.Value1.value;
            }

            return result;
        }
    }
}