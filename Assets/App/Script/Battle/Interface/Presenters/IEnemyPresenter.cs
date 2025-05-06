using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IEnemyPresenter
    {
        void Spawn(EnemyData enemyData, Pose spawnPose);

        int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance);
    }
}