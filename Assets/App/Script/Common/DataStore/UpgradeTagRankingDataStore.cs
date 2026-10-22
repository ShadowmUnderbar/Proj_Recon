using System.Collections.Generic;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using App.Common.Interface;
using VContainer;

namespace App.Common.DataStore
{
    /// <summary>
    /// アップグレードIDをマスターデータへ引き当て、<see cref="UpgradeTagRanking"/> でタグの件数順・所持件数を求める
    /// </summary>
    public class UpgradeTagRankingDataStore : IUpgradeTagRankingDataStore
    {
        private readonly UpgradeDatabase _upgradeDatabase;

        [Inject]
        public UpgradeTagRankingDataStore(UpgradeDatabase upgradeDatabase)
        {
            _upgradeDatabase = upgradeDatabase;
        }

        public IReadOnlyList<UpgradeTagCount> GetTopTags(IEnumerable<string> upgradeIds, int maxCount)
        {
            return UpgradeTagRanking.Top(ResolveUpgrades(upgradeIds), maxCount);
        }

        public int CountUpgrades(IEnumerable<string> upgradeIds)
        {
            return UpgradeTagRanking.CountUpgrades(ResolveUpgrades(upgradeIds));
        }

        /// <summary>ID をマスターデータへ引き当てる。見つからない ID は飛ばす</summary>
        private List<UpgradeMasterData> ResolveUpgrades(IEnumerable<string> upgradeIds)
        {
            var upgrades = new List<UpgradeMasterData>();
            if (upgradeIds != null)
            {
                foreach (var id in upgradeIds)
                {
                    if (_upgradeDatabase.TryGetUpgradeMasterData(id, out var upgrade))
                    {
                        upgrades.Add(upgrade);
                    }
                }
            }

            return upgrades;
        }
    }
}
