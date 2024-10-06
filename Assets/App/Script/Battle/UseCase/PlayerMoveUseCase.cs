using VContainer;
using VContainer.Unity;
using App.Battle.Interface;
using App.Common.Interface;

namespace App.Battle.UseCase
{
    public class PlayerMoveUseCase : IPlayerMoveUseCase, ITickable
    {
        private IPlayerControlPresenter _playerControlPresenter;
        private IGameInputUsecase _gameInputUsecase;

        private readonly float _speed = 0.05f;

        [Inject]
        public PlayerMoveUseCase(
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUsecase
        )
        {
            _playerControlPresenter = playerControlPresenter;
            _gameInputUsecase = gameInputUsecase;
        }

        public void Tick()
        {
            _playerControlPresenter.Move(_gameInputUsecase.V2LeftAxis, _speed);
        }
    }
}