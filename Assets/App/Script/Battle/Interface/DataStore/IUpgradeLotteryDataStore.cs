using System.Collections.Generic;
using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    public interface IUpgradeLotteryDataStore
    {
        IReadOnlyList<UpgradeMasterData> DrawUpgrades(int count);
    }
}
