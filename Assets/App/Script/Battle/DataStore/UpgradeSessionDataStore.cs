using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeSessionDataStore : IUpgradeSessionDataStore
    {
        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly List<string> _appliedUpgrades = new();
        public IReadOnlyList<string> AppliedUpgrades => _appliedUpgrades;

        [Inject]
        public UpgradeSessionDataStore(
            UpgradeDatabase upgradeDatabase
        )
        {
            _upgradeDatabase = upgradeDatabase;
        }

        public float GetUpgradeValue(UpgradeType upgradeType)
        {
            var value = 1f;

            foreach (var upgrade in _appliedUpgrades)
            {
                if (!_upgradeDatabase.TryGetUpgradeMasterData(upgrade, out var upgradeMasterData))
                {
                    continue;
                }

                if (upgradeMasterData.UpgradeType == upgradeType)
                {
                    value += upgradeMasterData.Value;
                }
            }

            return value;
        }

        public void AddUpgrade(UpgradeMasterData upgradeData)
        {
            if (_appliedUpgrades.Contains(upgradeData.Id))
            {
                return;
            }

            _appliedUpgrades.Add(upgradeData.Id);
        }

        public void Reset()
        {
            _appliedUpgrades.Clear();
        }
    }
}