using App.Script.Battle.Data;

namespace App.Script.Battle.Interface.DataStore
{
    public interface IEnemyDataStore
    {
        bool TryGetEnemyData(uint enemyId, out EnemyData enemyData);
        uint AddEnemyData(EnemyData enemyData);
        bool RemoveEnemyData(uint enemyId);
    }
}