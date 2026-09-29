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
    /// プレイヤーの移動・回避・射撃は各UseCaseが入口で <see cref="IFreezeDataStore.IsFreezing"/> を見て止める。
    /// 敵の停止はウェーブ間ポーズ・フリーズ・オーバークロックのいずれかで掛かるため、その判定もここに一本化する
    /// （停止要因ごとに別々に SetPause すると、片方の解除でもう片方の停止を解いてしまう）。
    /// </summary>
    public class FreezeUseCase : IInitializable, IDisposable
    {
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly ITracerFreezeState _tracerFreezeState;
        private readonly IOverclockDataStore _overclockDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public FreezeUseCase(
            IFreezeDataStore freezeDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyPresenter enemyPresenter,
            IBulletStoreView bulletStoreView,
            ITracerFreezeState tracerFreezeState,
            IOverclockDataStore overclockDataStore
        )
        {
            _freezeDataStore = freezeDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _enemyPresenter = enemyPresenter;
            _bulletStoreView = bulletStoreView;
            _tracerFreezeState = tracerFreezeState;
            _overclockDataStore = overclockDataStore;
        }

        public void Initialize()
        {
            _freezeDataStore.IsFreezing
                .Subscribe(OnFreezeChanged)
                .AddTo(_disposable);

            _waveManagerDataStore.IsWavePause
                .CombineLatest(_freezeDataStore.IsFreezing, _overclockDataStore.IsActive,
                    (isWavePause, isFreezing, isOverclock) => isWavePause || isFreezing || isOverclock)
                .DistinctUntilChanged()
                .Subscribe(_enemyPresenter.SetPause)
                .AddTo(_disposable);
        }

        private void OnFreezeChanged(bool isFreezing)
        {
            _bulletStoreView.SetPause(isFreezing);

            // 即着弾のレイ（曳光弾・カウンターのレイ）の保持・収縮も止める
            _tracerFreezeState.SetFreezing(isFreezing);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
