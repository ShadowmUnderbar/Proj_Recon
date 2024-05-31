using App.Script.Battle.Data;
using App.Script.Common.Data.MasterData;

namespace App.Script.Battle.Interface.DataStore
{
    public interface IEnemyDataStore
    {
        bool TryGetEnemyData(uint enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData);
        bool RemoveEnemyData(uint enemyId);
    }
}