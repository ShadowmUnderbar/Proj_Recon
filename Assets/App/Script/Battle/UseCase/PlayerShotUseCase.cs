using App.Battle.Interface.UseCase;
using App.Common.Interface.UseCase;
using App.Battle.Interface.Presenters;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace App.Battle.UseCase
{
    public class PlayerShotUseCase : IPlayerShotUseCase, ITickable
    {
        private IPlayerControlPresenter _playerControlPresenter;
        private IGameInputUsecase _gameInputUsecase;

        private const float FireRate = 0.1f;
        private float _leftFireTime = float.PositiveInfinity;
        private float _rightFireTime = float.PositiveInfinity;

        [Inject]
        public void Construct(
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUsecase
        )
        {
            _playerControlPresenter = playerControlPresenter;
            _gameInputUsecase = gameInputUsecase;
        }

        public void Tick()
        {
            _leftFireTime += Time.deltaTime;
            _rightFireTime += Time.deltaTime;
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
            if(_leftFireTime < FireRate)
            {
                return;
            }

            _leftFireTime = 0;
            _playerControlPresenter.Shot(true);

        }

        private void TryRightShot()
        {
            if (_rightFireTime < FireRate)
            {
                return;
            }

            _rightFireTime = 0;
            _playerControlPresenter.Shot(false);
        }
    }
}