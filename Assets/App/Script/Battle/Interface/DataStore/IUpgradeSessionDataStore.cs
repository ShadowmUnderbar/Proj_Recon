using System.Collections.Generic;
using App.Common.Data;
using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeSessionDataStore
    {
        List<string> AppliedUpgrades { get; }
        float GetUpgradeValue(UpgradeType upgradeType);
        void AddUpgrade(UpgradeMasterData upgradeData);
        void Reset();
    }
}