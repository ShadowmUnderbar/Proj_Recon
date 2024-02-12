using App.Battle.Interface.UseCase;
using App.Common.Interface.UseCase;
using App.Battle.Interface.Presenters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class PlayerAimUseCase: IPlayerAimUseCase, ITickable
    {
        private IPlayerControlPresenter _playerControlPresenter;

        [Inject]
        public PlayerAimUseCase(
            IPlayerControlPresenter playerControlPresenter
        )
        {
            _playerControlPresenter = playerControlPresenter;
        }

        public void Tick()
        {
            _playerControlPresenter.Aim();
        }
    }
}