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

        public void Spawn(EnemyData enemyData)
        {
            _enemyStoreView.Spawn(enemyData).Forget();
        }

        public void UnSpawn(int enemyId)
        {
            _enemyStoreView.UnSpawn(enemyId);
        }

        public int[] GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance)
        {
            return _enemyStoreView.GetDodgeHitEnemies(playerPosition, direction, distance);
        }

        public void SetPlayerPose(Pose playerPose)
        {
            _enemyStoreView.SetPlayerPose(playerPose);
        }

        public UniTask Dead(int id)
        {
            return _enemyStoreView.Dead(id);
        }
    }
}