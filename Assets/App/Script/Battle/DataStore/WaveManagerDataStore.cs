using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    public class WaveManagerDataStore : IWaveManagerDataStore
    {
        public ReactiveProperty<bool> IsWavePause { get; } = new(false);

        public void SetWavePause(bool isPause)
        {
            IsWavePause.Value = isPause;
        }
    }
}