using System;
using App.Battle.Data;
using VContainer;
using VContainer.Unity;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using UnityEngine;

namespace App.Battle.UseCase
{
    public class PlayerMoveUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerMoveUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore,
            IWaveManagerDataStore waveManagerDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
            _waveManagerDataStore = waveManagerDataStore;
        }

        public void Initialize()
        {
            _playerStateDataStore.Position
                .DistinctUntilChanged()
                .Subscribe(pos => _playerControlPresenter.Move(new Vector2(pos.x, pos.z)))
                .AddTo(_disposable);
            _playerStateDataStore.PlayerTransform =
                _playerControlPresenter.PlayerTransform;
        }

        public void Tick()
        {
            // ウェーブ間ポーズ中は移動を停止（移動モーションも止める）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                _playerControlPresenter.SetMoveAnimation(Vector2.zero);
                return;
            }

            // MoveSpeedは既にBaseSpeed * BasePlayerParameter.MoveSpeedを含むため、そのまま使用
            _playerStateDataStore.Move(_gameInputDataStore.V2LeftAxis,
                _playerStateDataStore.MoveSpeed);

            // ワールド入力を渡し、View側でモデルの向き基準にローカル化して移動モーションへ反映
            _playerControlPresenter.SetMoveAnimation(_gameInputDataStore.V2LeftAxis);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
