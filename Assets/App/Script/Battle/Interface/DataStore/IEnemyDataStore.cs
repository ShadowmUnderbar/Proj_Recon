using App.Common.Data.MasterData;
using App.Battle.Data;

namespace App.Battle.Interface.DataStore
{
    public interface IEnemyDataStore
    {
        bool TryGetEnemyData(int enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData);
        bool RemoveEnemyData(int enemyId);
    }
}