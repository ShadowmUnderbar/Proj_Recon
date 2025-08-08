using App.Battle.Interface;
using App.Battle.Data;
using App.Common.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.Presenters
{
    public class EnemyPresenter : IEnemyPresenter
    {
        private readonly IEnemyStoreView _enemyStoreView;

        public Observable<(int id, Pose pose)> OnEnemyPoseUpdate => _enemyStoreView.OnEnemyPoseUpdate;

        [Inject]
        public EnemyPresenter(
            IEnemyStoreView enemyStoreView
        )
        {
            _enemyStoreView = enemyStoreView;
        }

        public void Spawn(EnemyData enemyData, string prefabPath, HitDirectionType resistanceDirectionType)
        {
            _enemyStoreView.Spawn(enemyData, prefabPath, resistanceDirectionType).Forget();
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

        public void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2)
        {
            _enemyStoreView.SetPlayerAimDirection(aimDir1, aimDir2);
        }

        public UniTask Dead(int id)
        {
            return _enemyStoreView.Dead(id);
        }
    }
}