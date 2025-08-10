using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemyRandomSpawnUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyRandomSpawnCycleDataStore _enemyRandomSpawnCycleDataStore;
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public EnemyRandomSpawnUseCase(
            IEnemyRandomSpawnCycleDataStore enemyRandomSpawnCycleDataStore,
            IPlayerDataStore playerDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _enemyRandomSpawnCycleDataStore = enemyRandomSpawnCycleDataStore;
            _playerDataStore = playerDataStore;
            _enemyDataStore = enemyDataStore;
        }

        public void Initialize()
        {
            _enemyRandomSpawnCycleDataStore.OnSpawnCommonEnemy
                .Subscribe(x => SpawnRandomEnemy(x, EnemyRankType.Common))
                .AddTo(_disposables);

            _enemyRandomSpawnCycleDataStore.OnSpawnMinorEnemy
                .Subscribe(x => SpawnRandomEnemy(x, EnemyRankType.Minor))
                .AddTo(_disposables);
        }

        private void SpawnRandomEnemy(int enemyCount, EnemyRankType rankType)
        {
            var playerPos = _playerDataStore.Position.Value;
            for (var i = 0; i < enemyCount; i++)
            {
                var targetPos = _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);

                var enemy = _enemyDataStore.GetRandomEnemyMasterData(rankType);

                if (!_enemyDataStore.TryGetEnemyMasterData(enemy.EnemyCode, out var enemyMasterData))
                {
                    return;
                }

                _enemyDataStore.AddEnemyData(enemyMasterData, new Pose(targetPos, Quaternion.identity));
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}