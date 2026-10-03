using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;

namespace App.Battle.DataStore
{
    public class WaveManagerDataStore : IWaveManagerDataStore, IRunResettable
    {
        private readonly int _startWave;

        [Inject]
        public WaveManagerDataStore(DebugArenaSettings debugArenaSettings)
        {
            // デバッグ対戦では指定したウェーブから始める（敵の強さの倍率がそのウェーブ相当になる）
            _startWave = debugArenaSettings.StartWave;
            _currentWave = new ReactiveProperty<int>(_startWave);
        }

        public ReactiveProperty<bool> IsWavePause { get; } = new(false);

        private readonly ReactiveProperty<int> _currentWave;
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

        public void ResetRun()
        {
            _currentWave.Value = _startWave;
            _elapsedTime.Value = 0f;
            _killCount.Value = 0;

            // ポーズはビルド選択の開始・終了に合わせて RunStartUseCase が切り替えるため、ここでは触らない
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
