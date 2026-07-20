using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data.MasterData;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeSessionDataStore : IUpgradeSessionDataStore
    {
        // 所持している全アップグレード（読込分＋新規獲得分）
        private readonly List<string> _appliedUpgrades = new();

        // セット読込で最初から付与された分（新規獲得＝保存対象から除外するために記録）
        private readonly HashSet<string> _preloadedIds = new();

        public IReadOnlyList<string> AppliedUpgrades => _appliedUpgrades;

        public IReadOnlyList<string> NewlyAcquiredUpgrades =>
            _appliedUpgrades.Where(id => !_preloadedIds.Contains(id)).ToList();

        [Inject]
        public UpgradeSessionDataStore() { }

        public void AddUpgrade(UpgradeMasterData upgradeData)
        {
            if (_appliedUpgrades.Contains(upgradeData.Id))
            {
                return;
            }

            _appliedUpgrades.Add(upgradeData.Id);
        }

        public void Preload(UpgradeMasterData upgradeData)
        {
            if (_appliedUpgrades.Contains(upgradeData.Id))
            {
                return;
            }

            _appliedUpgrades.Add(upgradeData.Id);
            _preloadedIds.Add(upgradeData.Id);
        }

        public void Reset()
        {
            _appliedUpgrades.Clear();
            _preloadedIds.Clear();
        }
    }
}
