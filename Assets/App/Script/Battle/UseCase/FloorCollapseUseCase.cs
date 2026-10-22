using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 床崩壊の再生をゲーム進行につなぐ。
    /// ウェーブクリアでプレイヤーの足元を中心に崩壊を始め、次のウェーブが始まったら床を元に戻す。
    /// ショップは崩壊しきってから開く（<see cref="ShopUseCase"/> が <see cref="IFloorCollapseDataStore.OnCompleted"/> を待つ）。
    /// 崩壊中はプレイヤーを操作不能にする。移動・射撃・回避はウェーブ間ポーズで止まっているので、ここではフォーカスの切り替えを止める。
    /// 見た目の変形だけなので、コライダー・当たり判定には手を入れない。
    /// 崩壊しきった後の落下シーケンスは <see cref="IFloorCollapseDataStore.OnCompleted"/> から始める想定で、ここでは持たない
    /// </summary>
    public class FloorCollapseUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IFloorCollapseDataStore _floorCollapseDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IFloorCollapsePresenter _floorCollapsePresenter;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly FloorCollapseConfig _config;

        // 崩壊のためにフォーカス入力を止めたか。打ち切り・復帰で戻すときに、止めていなければ触らない
        private bool _isFocusInputLocked;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public FloorCollapseUseCase(
            IFloorCollapseDataStore floorCollapseDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IPlayerStateDataStore playerStateDataStore,
            IFloorCollapsePresenter floorCollapsePresenter,
            IGameInputDataStore gameInputDataStore,
            FloorCollapseConfig config
        )
        {
            _floorCollapseDataStore = floorCollapseDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _playerStateDataStore = playerStateDataStore;
            _floorCollapsePresenter = floorCollapsePresenter;
            _gameInputDataStore = gameInputDataStore;
            _config = config;
        }

        public void Initialize()
        {
            // 開始（やり直しを含む）でプレート床を敷き直し、操作を止める
            _floorCollapseDataStore.OnStarted
                .Subscribe(_ => OnStarted())
                .AddTo(_disposables);

            // 床を戻す（次のウェーブ開始・打ち切り）
            _floorCollapseDataStore.Phase
                .Where(phase => phase == FloorCollapsePhase.Idle)
                .Subscribe(_ => OnRestored())
                .AddTo(_disposables);

            _floorCollapseDataStore.Progress
                .Subscribe(progress => _floorCollapsePresenter.SetProgress(progress))
                .AddTo(_disposables);

            // ウェーブクリアでショップが開くのと同時に崩し始める
            // マテリアルが無いと何も描かれないまま床が消えてショップも待たされるので、再生しない
            _waveManagerDataStore.OnWaveAdvanced
                .Where(_ => _config.PlayOnWaveClear && _config.PlateMaterial != null)
                .Subscribe(_ => _floorCollapseDataStore.Begin(_playerStateDataStore.Position.Value, _config.Settings))
                .AddTo(_disposables);

            // 次のウェーブが始まったら床を戻す（崩壊の途中でも戻す）
            _waveManagerDataStore.IsWavePause
                .Where(isPause => !isPause)
                .Subscribe(_ => _floorCollapseDataStore.ResetCollapse())
                .AddTo(_disposables);
        }

        private void OnStarted()
        {
            _floorCollapsePresenter.Begin(_floorCollapseDataStore.Wave);
            _gameInputDataStore.SetFocusInputEnable(false);
            _isFocusInputLocked = true;
        }

        /// <summary>
        /// 床を戻す。崩壊しきった後（Completed）もショップ中はフォーカスの切り替えを止めたままにし、ここで戻す
        /// </summary>
        private void OnRestored()
        {
            _floorCollapsePresenter.Restore();
            UnlockFocusInput();
        }

        private void UnlockFocusInput()
        {
            if (!_isFocusInputLocked)
            {
                return;
            }

            _isFocusInputLocked = false;
            _gameInputDataStore.SetFocusInputEnable(true);
        }

        public void Tick()
        {
            _floorCollapseDataStore.Advance(UnityEngine.Time.deltaTime);
        }

        public void Dispose()
        {
            UnlockFocusInput();
            _disposables.Dispose();
        }
    }
}
