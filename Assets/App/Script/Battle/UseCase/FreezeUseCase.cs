using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// フリーズの開始・解除に合わせて、敵・弾・即着弾のレイ演出をその場で止める。
    /// ボスの時止め（<see cref="ITimeStopDataStore"/>）の間も弾とレイは止める（敵は止めない。ボスが動くため）。
    /// プレイヤーの移動・回避・射撃は各UseCaseが入口で <see cref="IFreezeDataStore.IsFreezing"/> を見て止める。
    /// </summary>
    public class FreezeUseCase : IInitializable, IDisposable
    {
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly ITracerFreezeState _tracerFreezeState;
        private readonly ITimeStopDataStore _timeStopDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public FreezeUseCase(
            IFreezeDataStore freezeDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyPresenter enemyPresenter,
            IBulletStoreView bulletStoreView,
            ITracerFreezeState tracerFreezeState,
            ITimeStopDataStore timeStopDataStore
        )
        {
            _freezeDataStore = freezeDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _enemyPresenter = enemyPresenter;
            _bulletStoreView = bulletStoreView;
            _tracerFreezeState = tracerFreezeState;
            _timeStopDataStore = timeStopDataStore;
        }

        public void Initialize()
        {
            _freezeDataStore.IsFreezing
                .Subscribe(OnFreezeChanged)
                .AddTo(_disposable);

            _timeStopDataStore.IsTimeStopped
                .Subscribe(_ => ApplyProjectilePause())
                .AddTo(_disposable);
        }

        private void OnFreezeChanged(bool isFreezing)
        {
            // ウェーブ間ポーズと同じ停止機構を使うため、解除時にポーズ中の停止を解いてしまわないよう論理和で渡す
            _enemyPresenter.SetPause(isFreezing || _waveManagerDataStore.IsWavePause.Value);

            ApplyProjectilePause();
        }

        /// <summary>弾と即着弾のレイ（曳光弾・カウンターのレイ）を、フリーズ中か時止め中なら止める</summary>
        private void ApplyProjectilePause()
        {
            // 片方の解除でもう片方の停止を解いてしまわないよう論理和で渡す
            var isPaused = _freezeDataStore.IsFreezing.CurrentValue || _timeStopDataStore.IsTimeStopped.CurrentValue;
            _bulletStoreView.SetPause(isPaused);
            _tracerFreezeState.SetFreezing(isPaused);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
