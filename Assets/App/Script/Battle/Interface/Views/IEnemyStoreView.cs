using App.Battle.Data;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace App.Battle.Interface
{
    public interface IEnemyStoreView
    {
        UniTask Spawn(EnemyData enemyData);
        void UnSpawn(int enemyId);
        UniTask Dead(int id);
        void AllDeadEnemies();
        int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);
        void SetPlayerPose(Pose playerPose);
    }
}