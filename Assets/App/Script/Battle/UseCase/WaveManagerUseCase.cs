using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class WaveManagerUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public WaveManagerUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IGameInputDataStore gameInputDataStore
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _gameInputDataStore = gameInputDataStore;
        }

        public void Initialize()
        {
            _waveManagerDataStore.IsWavePause
                .Subscribe(OnUpdateWavePause)
                .AddTo(_disposable);
        }

        private void OnUpdateWavePause(bool isPause)
        {
            _enemyPresenter.SetPause(isPause);
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }

        public void Tick()
        {
            if (!_gameInputDataStore.IsDodge.Value)
            {
                return;
            }

            _waveManagerDataStore.SetWavePause(!_waveManagerDataStore.IsWavePause.Value);
        }
    }
}