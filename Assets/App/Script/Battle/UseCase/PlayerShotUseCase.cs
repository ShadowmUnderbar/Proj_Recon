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
            var shotType = _playerDataStore.GetShotType(true);

            if (!_playerDataStore.CanShotCoolDown(true,shotType))
            {
                return;
            }

            _playerDataStore.SetLeftNormalShotCoolDown(_playerDataStore.NormalFireRate);
            _playerControlPresenter.Shot(shotType, true);
        }

        private void TryRightShot()
        {
            var shotType = _playerDataStore.GetShotType(false);

            if (!_playerDataStore.CanShotCoolDown(false, shotType))
            {
                return;
            }

            _playerDataStore.SetRightNormalShotCoolDown(_playerDataStore.NormalFireRate);
            _playerControlPresenter.Shot(shotType, false);
        }
    }
}