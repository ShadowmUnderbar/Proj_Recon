using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ボスの時止めの間、<see cref="TimeStopConfig"/> のセピア調（プレイヤー側の色変え）をかける。
    /// 弾・プレイヤーの停止は <see cref="FreezeUseCase"/> と各プレイヤーの UseCase が行う。
    /// </summary>
    public class TimeStopEffectUseCase : IInitializable, IDisposable
    {
        private readonly ITimeStopDataStore _timeStopDataStore;
        private readonly ISepiaToneDataStore _sepiaToneDataStore;
        private readonly TimeStopConfig _timeStopConfig;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public TimeStopEffectUseCase(
            ITimeStopDataStore timeStopDataStore,
            ISepiaToneDataStore sepiaToneDataStore,
            TimeStopConfig timeStopConfig
        )
        {
            _timeStopDataStore = timeStopDataStore;
            _sepiaToneDataStore = sepiaToneDataStore;
            _timeStopConfig = timeStopConfig;
        }

        public void Initialize()
        {
            // 初期値（時止めなし）は飛ばす。かけていないプリセットを外しても何も起きないが、余計な呼び出しを避ける
            _timeStopDataStore.IsTimeStopped
                .Skip(1)
                .Subscribe(OnTimeStopChanged)
                .AddTo(_disposable);
        }

        private void OnTimeStopChanged(bool isTimeStopped)
        {
            if (isTimeStopped)
            {
                _sepiaToneDataStore.Apply(_timeStopConfig.SepiaTonePreset);
                return;
            }

            _sepiaToneDataStore.Release(_timeStopConfig.SepiaTonePreset);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
