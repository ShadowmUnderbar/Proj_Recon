using System;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// サブスク: ウェーブクリア（次のウェーブへ進んだ瞬間＝ショップが開く前）に、所持キャッシュへボーナスを足す。
    /// ボス撃破でのクリア（ラン終了）ではウェーブが進まないので発動しない
    /// </summary>
    public class SubscribeUseCase : IInitializable, IDisposable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPointDataStore _pointDataStore;
        private readonly ISubscribeDataStore _subscribeDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public SubscribeUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IPointDataStore pointDataStore,
            ISubscribeDataStore subscribeDataStore
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _pointDataStore = pointDataStore;
            _subscribeDataStore = subscribeDataStore;
        }

        public void Initialize()
        {
            _waveManagerDataStore.OnWaveAdvanced
                .Subscribe(_ => OnWaveAdvanced())
                .AddTo(_disposables);
        }

        private void OnWaveAdvanced()
        {
            _pointDataStore.Add(_subscribeDataStore.GetWaveClearBonus(_pointDataStore.CurrentPoint.CurrentValue));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
