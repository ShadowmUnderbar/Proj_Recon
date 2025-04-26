using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using R3;

namespace App.Battle.UseCase
{
    public class PlayerShotUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUsecase _gameInputUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerShotUseCase(
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
            _playerDataStore.ShotType
                .DistinctUntilChanged()
                .Subscribe(OnUpdateShotType)
                .AddTo(_disposable);
        }

        private void OnUpdateShotType(ShotType shotType)
        {
            _playerControlPresenter.SetRayColor(ThemeColors.GetRayColor(shotType));
        }

        public void Tick()
        {
            if (_gameInputUseCase.IsLeftTrigger)
            {
                TryLeftShot();
            }

            if (_gameInputUseCase.IsRightTrigger)
            {
                TryRightShot();
            }
        }

        private void TryLeftShot()
        {
            var shotType = _playerDataStore.ShotType.Value;

            if (!_playerDataStore.CanLeftShot)
            {
                return;
            }

            var focusType = AimFocusType.NotFocus;
            _playerDataStore.SetCoolDownTime(HandType.Left, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Left, shotType, focusType, _playerDataStore.FocusLeftTargetId.Value);
        }

        private void TryRightShot()
        {
            var shotType = _playerDataStore.ShotType.Value;

            if (!_playerDataStore.CanRightShot)
            {
                return;
            }

            var focusType = AimFocusType.NotFocus;
            _playerDataStore.SetCoolDownTime(HandType.Right, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Right, shotType, focusType,
                _playerDataStore.FocusRightTargetId.Value);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}