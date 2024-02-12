using App.Battle.Interface.UseCase;
using App.Common.Interface.UseCase;
using App.Battle.Interface.Presenters;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class PlayerMoveUseCase : IPlayerMoveUseCase, ITickable
    {
        private IPlayerControlPresenter _playerControlPresenter;
        private IGameInputUsecase _gameInputUsecase;

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
            _playerControlPresenter.Move(_gameInputUsecase.V2LeftAxis);
        }
    }
}