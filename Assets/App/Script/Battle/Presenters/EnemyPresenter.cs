using System.Collections.Generic;
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

        public void RemoveAllEnemies()
        {
            _enemyStoreView.AllDeadEnemies();
        }

        public IReadOnlyList<int> GetDodgeHitEnemies(Vector3 playerPosition, Vector3 direction, float distance)
        {
            return _enemyStoreView.GetDodgeHitEnemies(playerPosition, direction, distance);
        }

        public IReadOnlyList<int> GetGazeEnemies(Vector3 origin, Vector3 direction, float radius, float distance)
        {
            return _enemyStoreView.GetGazeEnemies(origin, direction, radius, distance);
        }

        public IReadOnlyList<int> GetLineHitEnemies(Vector3 origin, Vector3 direction, float radius, float distance)
        {
            return _enemyStoreView.GetLineHitEnemies(origin, direction, radius, distance);
        }

        public void SetSpeedMultiplier(int enemyId, float multiplier)
        {
            _enemyStoreView.SetSpeedMultiplier(enemyId, multiplier);
        }

        public void SetStun(int enemyId, bool isStun)
        {
            _enemyStoreView.SetStun(enemyId, isStun);
        }

        public void KnockBack(int enemyId, Vector3 destination, float duration)
        {
            _enemyStoreView.KnockBack(enemyId, destination, duration);
        }

        public void PlayHitFeedback(int enemyId, Vector3 hitDirection)
        {
            _enemyStoreView.PlayHitFeedback(enemyId, hitDirection);
        }

        public void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2)
        {
            _enemyStoreView.SetPlayerAimDirection(aimDir1, aimDir2);
        }

        public UniTask Dead(int id)
        {
            return _enemyStoreView.Dead(id);
        }

        public void SetPause(bool isPause)
        {
            _enemyStoreView.SetPause(isPause);
        }
    }
}