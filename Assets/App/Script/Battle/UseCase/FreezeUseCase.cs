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
    /// 敵の停止はウェーブ間ポーズ・フリーズ・オーバークロックのいずれかで掛かるため、その判定もここに一本化する
    /// （停止要因ごとに別々に SetPause すると、片方の解除でもう片方の停止を解いてしまう）。
    /// ポイント粒子はオーバークロック中だけ止める（回収も吸い寄せもさせない）。
    /// </summary>
    public class FreezeUseCase : IInitializable, IDisposable
    {
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IBulletStoreView _bulletStoreView;
        private readonly ITracerFreezeState _tracerFreezeState;
        private readonly IOverclockDataStore _overclockDataStore;
        private readonly ITimeStopDataStore _timeStopDataStore;
        private readonly IPointParticlePresenter _pointParticlePresenter;
        private readonly IFloorCollapseDataStore _floorCollapseDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public FreezeUseCase(
            IFreezeDataStore freezeDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IEnemyPresenter enemyPresenter,
            IBulletStoreView bulletStoreView,
            ITracerFreezeState tracerFreezeState,
            IOverclockDataStore overclockDataStore,
            ITimeStopDataStore timeStopDataStore,
            IPointParticlePresenter pointParticlePresenter,
            IFloorCollapseDataStore floorCollapseDataStore
        )
        {
            _freezeDataStore = freezeDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _enemyPresenter = enemyPresenter;
            _bulletStoreView = bulletStoreView;
            _tracerFreezeState = tracerFreezeState;
            _overclockDataStore = overclockDataStore;
            _timeStopDataStore = timeStopDataStore;
            _pointParticlePresenter = pointParticlePresenter;
            _floorCollapseDataStore = floorCollapseDataStore;
        }

        public void Initialize()
        {
            _freezeDataStore.IsFreezing
                .Subscribe(_ => ApplyProjectilePause())
                .AddTo(_disposable);

            _waveManagerDataStore.IsWavePause
                .CombineLatest(_freezeDataStore.IsFreezing, _overclockDataStore.IsActive,
                    (isWavePause, isFreezing, isOverclock) => isWavePause || isFreezing || isOverclock)
                .DistinctUntilChanged()
                .Subscribe(_enemyPresenter.SetPause)
                .AddTo(_disposable);

            _timeStopDataStore.IsTimeStopped
                .Subscribe(_ => ApplyProjectilePause())
                .AddTo(_disposable);

            // 床崩壊の間は、弾と粒子をその場に止めて波が届くのを待たせる（粒子は回収もさせない）
            _floorCollapseDataStore.Phase
                .Subscribe(_ => ApplyProjectilePause())
                .AddTo(_disposable);

            _overclockDataStore.IsActive
                .CombineLatest(_floorCollapseDataStore.Phase,
                    (isOverclock, phase) => isOverclock || phase == FloorCollapsePhase.Collapsing)
                .DistinctUntilChanged()
                .Subscribe(_pointParticlePresenter.SetPause)
                .AddTo(_disposable);
        }

        /// <summary>弾と即着弾のレイ（曳光弾・カウンターのレイ）を、フリーズ中か時止め中か床崩壊中なら止める</summary>
        private void ApplyProjectilePause()
        {
            // 片方の解除でもう片方の停止を解いてしまわないよう論理和で渡す
            var isPaused = _freezeDataStore.IsFreezing.CurrentValue
                           || _timeStopDataStore.IsTimeStopped.CurrentValue
                           || _floorCollapseDataStore.Phase.CurrentValue == FloorCollapsePhase.Collapsing;
            _bulletStoreView.SetPause(isPaused);
            _tracerFreezeState.SetFreezing(isPaused);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
