using App.Battle.Interface.Presenters;
using App.Script.Battle.Data;
using App.Script.Battle.Interface.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Battle.Presenters
{
    public class EnemySpawnPresenter : IEnemySpawnPresenter
    {
        private readonly IEnemyStoreView _enemyStoreView;
        
        public EnemySpawnPresenter(
            IEnemyStoreView enemyStoreView
        )
        {
            _enemyStoreView = enemyStoreView;
        }

        public void Spawn(EnemyData enemyData, Pose spawnPose)
        {
            _enemyStoreView.Spawn(enemyData, spawnPose).Forget();
        }
    }
}