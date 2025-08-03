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
            _enemyDataStore.OnEnemyAdded
                .Subscribe(OnEnemyAdded)
                .AddTo(_disposables);

            _enemyDataStore.OnEnemyRemoved
                .Subscribe(OnEnemyRemoved)
                .AddTo(_disposables);

            Spawn("O-001", new Pose(Vector3.right, Quaternion.identity));
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
        }

        private void OnEnemyAdded(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                Debug.LogError($"Enemy data not found for ID: {enemyId}");
                return;
            }

            _enemyPresenter.Spawn(enemyData);
        }

        private void OnEnemyRemoved(int enemyId)
        {
            _enemyPresenter.UnSpawn(enemyId);
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