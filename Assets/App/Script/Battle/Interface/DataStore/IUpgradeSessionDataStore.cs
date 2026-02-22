using System.Collections.Generic;
using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeSessionDataStore
    {
        IReadOnlyList<UpgradeType> AppliedUpgrades { get; }
        float GetUpgradeValue(UpgradeType upgradeType);
        void AddUpgrade(UpgradeData upgradeData);
        void Reset();
    }
}
