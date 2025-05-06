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
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUseCase _gameInputUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerMoveUseCase(
            IPlayerDataStore playerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUseCase gameInputUseCase
        )
        {
            _playerDataStore = playerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputUseCase = gameInputUseCase;
        }

        public void Initialize()
        {
            _playerDataStore.Position
                .DistinctUntilChanged()
                .Subscribe(pos => _playerControlPresenter.Move(new Vector2(pos.x, pos.z)))
                .AddTo(_disposable);
        }

        public void Tick()
        {
            _playerDataStore.Move(_gameInputUseCase.V2LeftAxis,
                _playerDataStore.MoveSpeed * BasePlayerParameter.MoveSpeed);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}