using App.Battle.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemyPresenter
    {
        void Spawn(EnemyData enemyData);
        void UnSpawn(int enemyId);

        int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);
        void SetPlayerPose(Pose playerPose);
        UniTask Dead(int id);
    }
}