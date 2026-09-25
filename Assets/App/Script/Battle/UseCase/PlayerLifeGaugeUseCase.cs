using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// プレイヤーのHP・バリア残量を足元の半円ゲージへ反映し、ゲージをプレイヤー位置に追従させる。
    /// </summary>
    public class PlayerLifeGaugeUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IPlayerBarrierDataStore _playerBarrierDataStore;
        private readonly IPlayerLifeGaugePresenter _playerLifeGaugePresenter;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerLifeGaugeUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IPlayerBarrierDataStore playerBarrierDataStore,
            IPlayerLifeGaugePresenter playerLifeGaugePresenter
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _playerBarrierDataStore = playerBarrierDataStore;
            _playerLifeGaugePresenter = playerLifeGaugePresenter;
        }

        public void Initialize()
        {
            // プレイヤー本体と同じ論理位置を購読して足元へ追従させる
            _playerStateDataStore.Position
                .Subscribe(_playerLifeGaugePresenter.SetPosition)
                .AddTo(_disposable);

            Observable.CombineLatest(_playerStateDataStore.Health, _playerStateDataStore.MaxHealth, ToRatio)
                .DistinctUntilChanged()
                .Subscribe(_playerLifeGaugePresenter.SetHealthRatio)
                .AddTo(_disposable);

            Observable.CombineLatest(_playerBarrierDataStore.CurrentBarrier, _playerBarrierDataStore.MaxBarrier,
                    ToRatio)
                .DistinctUntilChanged()
                .Subscribe(_playerLifeGaugePresenter.SetBarrierRatio)
                .AddTo(_disposable);

            // バリア未取得（最大値0）の間は外周の弧ごと隠す
            _playerBarrierDataStore.MaxBarrier
                .Select(max => max > 0f)
                .DistinctUntilChanged()
                .Subscribe(_playerLifeGaugePresenter.SetBarrierVisible)
                .AddTo(_disposable);
        }

        private static float ToRatio(float current, float max)
        {
            return max > 0f ? Math.Clamp(current / max, 0f, 1f) : 0f;
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
