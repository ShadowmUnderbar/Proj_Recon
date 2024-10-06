using App.Common.Data.MasterData;
using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IEnemyDataStore
    {
        bool TryGetEnemyData(uint enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData);
        bool RemoveEnemyData(uint enemyId);
    }
}