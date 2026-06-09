using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IWaveManagerDataStore
    {
        ReactiveProperty<bool> IsWavePause { get; }
        ReadOnlyReactiveProperty<int> CurrentWave { get; }
        ReadOnlyReactiveProperty<float> ElapsedTime { get; }
        ReadOnlyReactiveProperty<int> KillCount { get; }
        Observable<int> OnWaveAdvanced { get; }

        void SetWavePause(bool isPause);
        void AddElapsedTime(float deltaTime);
        void IncrementKillCount();
        void AdvanceWave();
    }
}
