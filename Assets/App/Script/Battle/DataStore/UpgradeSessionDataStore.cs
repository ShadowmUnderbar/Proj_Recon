using System.Collections.Generic;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class UpgradeSessionDataStore : IUpgradeSessionDataStore, IInitializable
    {
        private readonly UpgradeDatabase _upgradeDatabase;
        public List<string> AppliedUpgrades { get; } = new();

        [Inject]
        public UpgradeSessionDataStore(
            UpgradeDatabase upgradeDatabase
        )
        {
            _upgradeDatabase = upgradeDatabase;
        }

        public void Initialize()
        {
            _upgradeDatabase.TryGetUpgradeMasterData("BulletDamage001", out var upgradeMasterData);
            AddUpgrade(upgradeMasterData);
        }

        public float GetUpgradeValue(UpgradeType upgradeType)
        {
            var value = 0f;

            foreach (var upgrade in AppliedUpgrades)
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
            if (AppliedUpgrades.Contains(upgradeData.Id))
            {
                return;
            }

            AppliedUpgrades.Add(upgradeData.Id);
        }

        public void Reset()
        {
            AppliedUpgrades.Clear();
        }
    }
}