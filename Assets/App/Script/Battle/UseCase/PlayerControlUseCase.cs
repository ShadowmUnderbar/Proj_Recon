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
            Aim();
        }

        private void Move(Vector2 moveV2)
        {
            _playerControlPresenter.Move(moveV2);
        }

        private void Aim()
        {
            _playerControlPresenter.Aim();
        }
    }
}