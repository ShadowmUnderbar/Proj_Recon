using System;
using VContainer;
using VContainer.Unity;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;

namespace App.Battle.UseCase
{
    public class PlayerMoveUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUsecase _gameInputUseCase;

        private readonly CompositeDisposable _disposable = new();

        private const float Speed = 0.05f;

        [Inject]
        public PlayerMoveUseCase(
            IPlayerDataStore playerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUseCase
        )
        {
            _playerDataStore = playerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputUseCase = gameInputUseCase;
        }

        public void Initialize()
        {
            _playerControlPresenter.OnUpdatePosition
                .Subscribe(x => _playerDataStore.Position.Value = x)
                .AddTo(_disposable);
        }

        public void Tick()
        {
            _playerControlPresenter.Move(_gameInputUseCase.V2LeftAxis, Speed);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}