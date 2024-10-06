using App.Battle.Data;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace App.Battle.Interface
{
    public interface IEnemyStoreView
    {
        UniTask Spawn(EnemyData enemyData, Pose spawnPose);
    }
}