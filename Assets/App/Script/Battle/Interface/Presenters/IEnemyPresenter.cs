using App.Battle.Data;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemyPresenter
    {
        Observable<(int id, Pose pose)> OnEnemyPoseUpdate { get; }
        void Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType);
        void UnSpawn(int enemyId);
        void RemoveAllEnemies();

        int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);
        void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2);
        UniTask Dead(int id);
        void SetPause(bool isPause);
    }
}