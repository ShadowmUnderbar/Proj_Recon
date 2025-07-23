using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemySpawnUseCase
    {
        void Spawn(string enemyCode, Pose spawnPose);
        void SetPlayerPose(Pose playerPose);
    }
}