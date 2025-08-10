using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class EnemySpawnUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IEnemyDataStore _enemyDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public EnemySpawnUseCase(
            IEnemyPresenter enemyPresenter,
            IEnemyDataStore enemyDataStore
        )
        {
            _enemyPresenter = enemyPresenter;
            _enemyDataStore = enemyDataStore;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyAdded
                .Subscribe(OnEnemyAdded)
                .AddTo(_disposables);

            _enemyDataStore.OnEnemyRemoved
                .Subscribe(OnEnemyRemoved)
                .AddTo(_disposables);
        }

        private void OnEnemyAdded(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                Debug.LogError($"Enemy data not found for ID: {enemyId}");
                return;
            }

            if (!_enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyCode, out var enemyMasterData))
            {
                return;
            }

            _enemyPresenter.Spawn(enemyData, enemyMasterData.PrefabPath, enemyMasterData.ResistanceDirectionType);
        }

        private void OnEnemyRemoved(int enemyId)
        {
            _enemyPresenter.UnSpawn(enemyId);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}