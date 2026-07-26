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

        /// <summary>
        /// 指定位置の半径 radius 以内の NavMesh 上の点を返す（チョークポイントの寄せ用）。
        /// サンプルに失敗した場合は origin をそのまま返す。
        /// </summary>
        Vector3 GetClusteredSpawnPositionFast(Vector3 origin, float radius);

        void ResetSpawnCycle();
    }
}