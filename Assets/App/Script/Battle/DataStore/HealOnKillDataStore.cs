using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using VContainer;

namespace App.Battle.DataStore
{
    public class HealOnKillDataStore : IHealOnKillDataStore
    {
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeDatabase _upgradeDatabase;

        [Inject]
        public HealOnKillDataStore(
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeDatabase upgradeDatabase
        )
        {
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeDatabase = upgradeDatabase;
        }

        /// <summary>
        /// 撃破した敵のランクに応じた回復量を返す。所持中の最高レベルの HealOnKill を採用（非スタック）。
        /// マイナー撃破時は Value1、メジャー撃破時は Value2。対象外ランク・未所持は 0。
        /// </summary>
        public float GetHealAmount(EnemyRankType rankType)
        {
            UpgradeMasterData best = null;
            foreach (var id in _upgradeSessionDataStore.AppliedUpgrades)
            {
                if (!_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                {
                    continue;
                }

                if (data.UpgradeType != UpgradeType.HealOnKill)
                {
                    continue;
                }

                if (best == null || data.Level > best.Level)
                {
                    best = data;
                }
            }

            if (best == null)
            {
                return 0f;
            }

            return rankType switch
            {
                EnemyRankType.Minor => best.Value1.value,
                EnemyRankType.Major => best.Value2.value,
                _ => 0f
            };
        }
    }
}
