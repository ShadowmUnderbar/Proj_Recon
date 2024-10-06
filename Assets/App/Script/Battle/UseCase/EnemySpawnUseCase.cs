using App.Battle.Interface;
using App.Common.Data.Database;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemySpawnUseCase : IEnemySpawnUseCase, IInitializable
    {
        private readonly EnemyDatabase _enemyDatabase;
        private readonly IEnemySpawnPresenter _enemySpawnPresenter;
        private readonly IEnemyDataStore _enemyDataStore;

        [Inject]
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
            Spawn("TestEnemy", new Pose(Vector3.right, Quaternion.identity));
        }
    }
}