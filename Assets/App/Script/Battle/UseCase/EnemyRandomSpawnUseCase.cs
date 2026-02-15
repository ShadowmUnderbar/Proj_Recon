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
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public EnemyRandomSpawnUseCase(
            IEnemyRandomSpawnCycleDataStore enemyRandomSpawnCycleDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _enemyRandomSpawnCycleDataStore = enemyRandomSpawnCycleDataStore;
            _playerStateDataStore = playerStateDataStore;
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
            var playerPos = _playerStateDataStore.Position.Value;
            for (var i = 0; i < enemyCount; i++)
            {
                var targetPos = _enemyRandomSpawnCycleDataStore.GetRandomSpawnPositionFast(playerPos);

                if (!_enemyDataStore.TryGetRandomEnemyMasterData(rankType, _playerStateDataStore.UnlockCoreSkillType, out var enemy))
                {
                    return;
                }

                _enemyDataStore.AddEnemyData(enemy, new Pose(targetPos, Quaternion.identity));
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
