using App.Battle.Interface;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using App.Common.Interface;
using App.Battle.DataStore;

namespace App.Battle.UseCase
{
    public class PlayerShotUseCase : IPlayerShotUseCase, ITickable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUsecase _gameInputUsecase;

        [Inject]
        public PlayerShotUseCase(
            IPlayerDataStore playerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUsecase
        )
        {
            _playerDataStore = playerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputUsecase = gameInputUsecase;
        }

        public void Tick()
        {
            if (_gameInputUsecase.IsLeftTrigger)
            {
                TryLeftShot();
            }
            
            if (_gameInputUsecase.IsRightTrigger)
            {
                TryRightShot();
            }
        }

        private void TryLeftShot()
        {
            if(!_playerDataStore.CanLeftNormalShot)
            {
                return;
            }

            _playerDataStore.SetLeftNormalShotCoolDown(_playerDataStore.NormalFireRate);
            _playerControlPresenter.Shot(true);

        }

        private void TryRightShot()
        {
            if (_playerDataStore.CanRightNormalShot)
            {
                return;
            }

            _playerDataStore.SetRightNormalShotCoolDown(_playerDataStore.NormalFireRate);
            _playerControlPresenter.Shot(false);
        }
    }
}