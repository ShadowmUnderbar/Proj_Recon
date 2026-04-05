using App.Battle.Data;
using App.Common.Data;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;

namespace App.Battle.Interface
{
    public interface IEnemyStoreView
    {
        Observable<(int id, Pose pose)> OnEnemyPoseUpdate { get; }
        UniTask Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType);
        void UnSpawn(int enemyId);
        UniTask Dead(int id);
        void AllDeadEnemies();
        int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);
        void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2);
        void SetPause(bool isPause);
    }
}