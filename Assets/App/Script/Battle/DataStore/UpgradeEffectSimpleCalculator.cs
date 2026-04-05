using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeEffectSimpleCalculatorDataStore : IUpgradeEffectSimpleCalculatorDataStore
    {
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeDatabase _upgradeDatabase;

        [Inject]
        public UpgradeEffectSimpleCalculatorDataStore(
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeDatabase upgradeDatabase
        )
        {
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeDatabase = upgradeDatabase;
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

                result += data.Value1.value;
            }

            return result;
        }
    }
}