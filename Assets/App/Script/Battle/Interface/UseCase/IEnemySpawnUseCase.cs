using UnityEngine;

namespace App.Script.Battle.Interface.UseCase
{
    public interface IEnemySpawnUseCase
    {
        void Spawn(uint enemyId, Pose spawnPose);
    }
}