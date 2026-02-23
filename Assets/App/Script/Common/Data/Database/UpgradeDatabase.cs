using System;
using System.Linq;
using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Database/UpgradeDatabase")]
    public class UpgradeDatabase : ScriptableObject
    {
        [SerializeField] private UpgradeMasterData[] _upgradeMasterData;

        public UpgradeMasterData[] UpgradeMasterData => _upgradeMasterData;

        public bool TryGetUpgradeMasterData(string id, out UpgradeMasterData upgradeMasterData)
        {
            upgradeMasterData =
                _upgradeMasterData.FirstOrDefault(upgradeMasterData => upgradeMasterData.Id == id);
            return upgradeMasterData != null;
        }

        public UpgradeMasterData[] GetSameLevelUpgrades(string id)
        {
            if (!TryGetUpgradeMasterData(id, out var upgradeMasterData))
            {
                return Array.Empty<UpgradeMasterData>();
            }

            var sameLevelUpgrades = _upgradeMasterData.Where(data =>
                data.Level == upgradeMasterData.Level).ToArray();

            return sameLevelUpgrades;
        }
    }
}