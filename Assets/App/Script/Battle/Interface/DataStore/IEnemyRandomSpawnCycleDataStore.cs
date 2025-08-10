using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IEnemyRandomSpawnCycleDataStore
    {
        Observable<int> OnSpawnCommonEnemy { get; }
        Observable<int> OnSpawnMinorEnemy { get; }
        Observable<Unit> OnSpawnMajorEnemy { get; }
        Observable<Unit> OnSpawnBossEnemy { get; }
        Observable<Unit> OnSpawnIrregularEnemy { get; }
        Vector3 GetRandomSpawnPositionFast(Vector3 playerPosition);
    }
}