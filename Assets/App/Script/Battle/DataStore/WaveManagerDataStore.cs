using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    public class WaveManagerDataStore : IWaveManagerDataStore
    {
        public ReactiveProperty<bool> IsWavePause { get; } = new(false);

        private readonly ReactiveProperty<int> _currentWave = new(1);
        public ReadOnlyReactiveProperty<int> CurrentWave => _currentWave;

        private readonly ReactiveProperty<float> _elapsedTime = new(0f);
        public ReadOnlyReactiveProperty<float> ElapsedTime => _elapsedTime;

        private readonly ReactiveProperty<int> _killCount = new(0);
        public ReadOnlyReactiveProperty<int> KillCount => _killCount;

        private readonly Subject<int> _onWaveAdvanced = new();
        public Observable<int> OnWaveAdvanced => _onWaveAdvanced;

        public void SetWavePause(bool isPause)
        {
            IsWavePause.Value = isPause;
        }

        public void AddElapsedTime(float deltaTime)
        {
            _elapsedTime.Value += deltaTime;
        }

        public void IncrementKillCount()
        {
            _killCount.Value++;
        }

        public void AdvanceWave()
        {
            _currentWave.Value++;
            _elapsedTime.Value = 0f;
            _killCount.Value = 0;
            _onWaveAdvanced.OnNext(_currentWave.Value);
        }
    }
}
