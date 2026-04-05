using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IWaveManagerDataStore
    {
        ReactiveProperty<bool> IsWavePause { get; }

        void SetWavePause(bool isPause);
    }
}