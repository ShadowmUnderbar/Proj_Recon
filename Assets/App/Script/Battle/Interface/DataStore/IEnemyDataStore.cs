using System.Collections.Generic;
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

        /// <summary>撃破の決め手となった命中情報を流す（撃破フォームを参照するバフの駆動に使う）</summary>
        Observable<HitData> OnEnemyDeadByHit { get; }
        List<EnemyData> Enemies { get; }

        bool TryGetEnemyMasterData(string enemyCode, out EnemyMasterData enemyMasterData);

        bool TryGetRandomEnemyMasterData(EnemyRankType rankType, UnlockCoreSkillType unlockCoreSkillType,
            out EnemyMasterData enemyMasterData);

        bool TryGetEnemyData(int enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData, Pose spawnPose);
        bool RemoveEnemyData(int enemyId);
        void RemoveAllEnemyData();
        void Damage(HitData hitData);
        void UpdateEnemyPose(int id, Pose pose);
    }
}