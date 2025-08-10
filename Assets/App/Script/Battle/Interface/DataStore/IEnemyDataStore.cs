using App.Common.Data.MasterData;
using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IEnemyDataStore
    {
        Observable<int> OnEnemyAdded { get; }
        Observable<int> OnEnemyRemoved { get; }
        Observable<int> OnEnemyDead { get; }
        bool TryGetEnemyMasterData(string enemyCode, out EnemyMasterData enemyMasterData);
        EnemyMasterData GetRandomEnemyMasterData(EnemyRankType rankType);
        bool TryGetEnemyData(int enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData, Pose spawnPose);
        bool RemoveEnemyData(int enemyId);
        void Damage(HitData hitData);
        void UpdateEnemyPose(int id, Pose pose);
    }
}