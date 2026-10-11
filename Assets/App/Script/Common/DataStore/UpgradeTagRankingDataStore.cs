using System.Collections.Generic;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using App.Common.Interface;
using VContainer;

namespace App.Common.DataStore
{
    /// <summary>
    /// アップグレードIDをマスターデータへ引き当て、<see cref="UpgradeTagRanking"/> でタグの件数順を求める
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

            return UpgradeTagRanking.Top(upgrades, maxCount);
        }
    }
}
