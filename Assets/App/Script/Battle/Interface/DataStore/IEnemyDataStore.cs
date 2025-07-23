using App.Common.Data.MasterData;
using App.Battle.Data;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IEnemyDataStore
    {
        Observable<int> OnEnemyAdded { get; }
        Observable<int> OnEnemyRemoved { get; }
        Observable<int> OnEnemyDead { get; }
        bool TryGetEnemyData(int enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData);
        bool RemoveEnemyData(int enemyId);
        void Damage(int enemyId, float damage);
    }
}