using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemySpawnUseCase : IEnemySpawnUseCase, IInitializable, ITickable, IDisposable
    {
        private readonly EnemyDatabase _enemyDatabase;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerDataStore _playerDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public EnemySpawnUseCase(
            EnemyDatabase enemyDatabase,
            IEnemyPresenter enemyPresenter,
            IEnemyDataStore enemyDataStore,
            IPlayerDataStore playerDataStore
        )
        {
            _enemyDatabase = enemyDatabase;
            _enemyPresenter = enemyPresenter;
            _enemyDataStore = enemyDataStore;
            _playerDataStore = playerDataStore;
        }

        public void Initialize()
        {
            Spawn("TestEnemy", new Pose(Vector3.right, Quaternion.identity));
        }

        public void Tick()
        {
            SetPlayerPose(_playerDataStore.Pose);
        }

        public void Spawn(string enemyCode, Pose spawnPose)
        {
            if (!_enemyDatabase.TryGetEnemyMasterData(enemyCode, out var enemyMasterData))
            {
                return;
            }

            var enemy = _enemyDataStore.AddEnemyData(enemyMasterData);

            _enemyPresenter.Spawn(enemy, spawnPose);
        }

        public void SetPlayerPose(Pose playerPose)
        {
            _enemyPresenter.SetPlayerPose(playerPose);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}