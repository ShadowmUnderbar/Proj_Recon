using UnityEngine;

namespace App.Script.Battle.Interface.UseCase
{
    public interface IEnemySpawnUseCase
    {
        void Spawn(string enemyCode, Pose spawnPose);
    }
}