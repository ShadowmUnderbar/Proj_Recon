using App.Battle.Interface.UseCase;
using App.Common.Interface.UseCase;
using App.Battle.Interface.Presenters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class PlayerControlUseCase : IPlayerControlUseCase, ITickable
    {
        private IPlayerControlPresenter _playerControlPresenter;
        private IGameInputUsecase _gameInputUsecase;

        [Inject]
        public PlayerControlUseCase(
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUsecase
        )
        {
            _playerControlPresenter = playerControlPresenter;
            _gameInputUsecase = gameInputUsecase;
        }

        public void Tick()
        {
            Move(_gameInputUsecase.V2LeftAxis);
        }

        public void Move(Vector2 moveV2)
        {
            Debug.Log(moveV2);
            //_playerControlPresenter.Move(moveV2);
        }

        public void AimLeft(Vector2 position)
        {
            _playerControlPresenter.AimLeft(position);
        }

        public void AimRight(Vector2 position)
        {
            _playerControlPresenter.AimRight(position);
        }
    }
}