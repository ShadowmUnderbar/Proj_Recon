using System.Collections.Generic;
using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeSessionDataStore
    {
        IReadOnlyList<string> AppliedUpgrades { get; }
        void AddUpgrade(UpgradeMasterData upgradeData);
        void Reset();
    }
}