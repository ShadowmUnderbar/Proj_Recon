using App.Battle.Interface.Presenters;
using App.Script.Battle.Interface.DataStore;
using App.Script.Battle.Interface.UseCase;
using App.Script.Common.Data.Database;
using UnityEngine;
using VContainer.Unity;

namespace App.Script.Battle.UseCase
{
    public class EnemySpawnUseCase : IEnemySpawnUseCase, IInitializable
    {
        private readonly EnemyDatabase _enemyDatabase;
        private readonly IEnemySpawnPresenter _enemySpawnPresenter;
        private readonly IEnemyDataStore _enemyDataStore;

        public EnemySpawnUseCase(
            EnemyDatabase enemyDatabase,
            IEnemySpawnPresenter enemySpawnPresenter,
            IEnemyDataStore enemyDataStore
        )
        {
            _enemyDatabase = enemyDatabase;
            _enemySpawnPresenter = enemySpawnPresenter;
            _enemyDataStore = enemyDataStore;
        }

        public void Spawn(string enemyCode, Pose spawnPose)
        {
            if (!_enemyDatabase.TryGetEnemyMasterData(enemyCode, out var enemyMasterData))
            {
                return;
            }

            var enemy = _enemyDataStore.AddEnemyData(enemyMasterData);

            _enemySpawnPresenter.Spawn(enemy, spawnPose);
        }

        public void Initialize()
        {
            Spawn("TestEnemy", new Pose(Vector3.zero, Quaternion.identity));
        }
    }
}