using App.Script.Battle.Data;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace App.Script.Battle.Interface.Views
{
    public interface IEnemyStoreView
    {
        UniTask Spawn(EnemyData enemyData, Pose spawnPose);
    }
}