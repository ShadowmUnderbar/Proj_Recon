using App.Script.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface.Presenters
{
    public interface IEnemySpawnPresenter
    {
        void Spawn(EnemyData enemyData, Pose spawnPose);
    }
}