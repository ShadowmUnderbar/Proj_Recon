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

        /// <summary>ダメージが通った敵のIDを流す（撃破に至らない命中も含む）</summary>
        Observable<int> OnEnemyDamaged { get; }
        List<EnemyData> Enemies { get; }

        bool TryGetEnemyMasterData(string enemyCode, out EnemyMasterData enemyMasterData);

        bool TryGetRandomEnemyMasterData(EnemyRankType rankType, UnlockCoreSkillType unlockCoreSkillType,
            out EnemyMasterData enemyMasterData);

        bool TryGetEnemyData(int enemyId, out EnemyData enemyData);
        EnemyData AddEnemyData(EnemyMasterData enemyMasterData, Pose spawnPose);
        bool RemoveEnemyData(int enemyId);
        void RemoveAllEnemyData();
        void Damage(HitData hitData);

        /// <summary>
        /// 指定の敵どうしで体力を共有させる（複数個体のボス）。共有体力は各敵の現在の体力の合計。
        /// 誰に当てても共有体力が減り、0になると当てた敵は通常どおり撃破（OnEnemyDead）、
        /// 残りは撃破扱いにせず消す（OnEnemyRemoved。ポイント・撃破数は1体ぶん）
        /// </summary>
        void LinkSharedHealth(IReadOnlyList<int> enemyIds);
        void UpdateEnemyPose(int id, Pose pose);
    }
}