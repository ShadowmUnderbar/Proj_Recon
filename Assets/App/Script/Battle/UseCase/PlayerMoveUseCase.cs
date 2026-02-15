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

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerMoveUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
        }

        public void Initialize()
        {
            _playerStateDataStore.Position
                .DistinctUntilChanged()
                .Subscribe(pos => _playerControlPresenter.Move(new Vector2(pos.x, pos.z)))
                .AddTo(_disposable);
        }

        public void Tick()
        {
            // MoveSpeedは既にBaseSpeed * BasePlayerParameter.MoveSpeedを含むため、そのまま使用
            _playerStateDataStore.Move(_gameInputDataStore.V2LeftAxis,
                _playerStateDataStore.MoveSpeed);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
