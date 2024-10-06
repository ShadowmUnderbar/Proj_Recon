using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemySpawnPresenter
    {
        void Spawn(EnemyData enemyData, Pose spawnPose);
    }
}