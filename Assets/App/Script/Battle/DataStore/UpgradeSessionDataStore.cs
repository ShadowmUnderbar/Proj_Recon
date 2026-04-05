using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data.MasterData;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeSessionDataStore : IUpgradeSessionDataStore
    {
        private readonly List<string> _appliedUpgrades = new();
        public IReadOnlyList<string> AppliedUpgrades => _appliedUpgrades;

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

        public void Reset()
        {
            _appliedUpgrades.Clear();
        }
    }
}