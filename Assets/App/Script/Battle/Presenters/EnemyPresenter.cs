using App.Battle.Interface;
using App.Battle.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace App.Battle.Presenters
{
    public class EnemyPresenter : IEnemyPresenter
    {
        private readonly IEnemyStoreView _enemyStoreView;

        [Inject]
        public EnemyPresenter(
            IEnemyStoreView enemyStoreView
        )
        {
            _enemyStoreView = enemyStoreView;
        }

        public void Spawn(EnemyData enemyData, Pose spawnPose)
        {
            _enemyStoreView.Spawn(enemyData, spawnPose).Forget();
        }

        public int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance)
        {
            return _enemyStoreView.GetDodgeHitEnemies(playerPosition, direction, distance);
        }
    }
}