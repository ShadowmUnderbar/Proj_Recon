using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 床崩壊の再生をゲーム進行につなぐ。
    /// ウェーブクリア（ショップを開く時点）でプレイヤーの足元を中心に崩壊を始め、次のウェーブが始まったら床を元に戻す。
    /// 見た目の変形だけなので、コライダー・移動・当たり判定には手を入れない（ウェーブ間ポーズ中は敵も弾も止まっている）。
    /// 崩壊しきった後の落下シーケンスは <see cref="IFloorCollapseDataStore.OnCompleted"/> から始める想定で、ここでは持たない
    /// </summary>
    public class FloorCollapseUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IFloorCollapseDataStore _floorCollapseDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IFloorCollapsePresenter _floorCollapsePresenter;
        private readonly FloorCollapseConfig _config;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public FloorCollapseUseCase(
            IFloorCollapseDataStore floorCollapseDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IFloorCollapsePresenter floorCollapsePresenter,
            FloorCollapseConfig config
        )
        {
            _floorCollapseDataStore = floorCollapseDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _playerStateDataStore = playerStateDataStore;
            _floorCollapsePresenter = floorCollapsePresenter;
            _config = config;
        }

        public void Initialize()
        {
            _floorCollapseDataStore.Phase
                .Subscribe(OnPhaseChanged)
                .AddTo(_disposables);

            _floorCollapseDataStore.Progress
                .Subscribe(progress => _floorCollapsePresenter.SetProgress(progress))
                .AddTo(_disposables);

            // ウェーブクリアでショップが開くのと同時に崩し始める
            _waveManagerDataStore.OnWaveAdvanced
                .Where(_ => _config.PlayOnWaveClear)
                .Subscribe(_ => _floorCollapseDataStore.Begin(_playerStateDataStore.Position.Value, _config.Settings))
                .AddTo(_disposables);

            // 次のウェーブが始まったら床を戻す（崩壊の途中でも戻す）
            _waveManagerDataStore.IsWavePause
                .Where(isPause => !isPause)
                .Subscribe(_ => _floorCollapseDataStore.ResetCollapse())
                .AddTo(_disposables);
        }

        private void OnPhaseChanged(FloorCollapsePhase phase)
        {
            switch (phase)
            {
                case FloorCollapsePhase.Collapsing:
                    _floorCollapsePresenter.Begin(_floorCollapseDataStore.Origin, _floorCollapseDataStore.Settings);
                    break;
                case FloorCollapsePhase.Idle:
                    _floorCollapsePresenter.Restore();
                    break;
            }
        }

        public void Tick()
        {
            _floorCollapseDataStore.Advance(UnityEngine.Time.deltaTime);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
