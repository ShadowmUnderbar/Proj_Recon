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

        /// <summary>
        /// 指定タイプの全アップグレードのうち Value1 の最大値を返す。
        /// レベルが累積せず「最高レベルのみ採用」したい効果（バリア等）に使う。効果なし時は 0f。
        /// </summary>
        public float CalcMax(UpgradeType upgradeType)
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

                if (data.Value1.value > result)
                {
                    result = data.Value1.value;
                }
            }

            return result;
        }

        /// <summary>
        /// 指定タイプで所持中の最高レベルを返す。効果なし時は 0。
        /// レベルによって挙動が変わる効果（ビッグマウスのLv3など）の判定に使う。
        /// </summary>
        public int CalcMaxLevel(UpgradeType upgradeType)
        {
            var result = 0;
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

                if (data.Level > result)
                {
                    result = data.Level;
                }
            }

            return result;
        }
    }
}