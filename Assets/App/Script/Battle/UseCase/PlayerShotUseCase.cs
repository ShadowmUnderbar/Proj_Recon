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
        private readonly IPlayerSettingDataStore _playerSettingDataStore;
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUseCase _gameInputUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerShotUseCase(
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerDataStore playerDataStore,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUseCase gameInputUseCase
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _playerDataStore = playerDataStore;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputUseCase = gameInputUseCase;
        }

        public void Initialize()
        {
            _playerDataStore.ShotType
                .DistinctUntilChanged()
                .Subscribe(_ => OnUpdateShotType())
                .AddTo(_disposable);
            _playerDataStore.RightFocusType
                .DistinctUntilChanged()
                .Subscribe(_ => OnUpdateShotType())
                .AddTo(_disposable);
            _playerDataStore.LeftFocusType
                .DistinctUntilChanged()
                .Subscribe(_ => OnUpdateShotType())
                .AddTo(_disposable);
        }

        private void OnUpdateShotType()
        {
            var shotType = _playerDataStore.ShotType.Value;
            var leftFocusType = _playerDataStore.LeftFocusType.Value;
            var rightFocusType = _playerDataStore.RightFocusType.Value;

            _playerControlPresenter.SetHandRayColor(HandType.Left, ThemeColors.GetRayColor(shotType, leftFocusType));
            _playerControlPresenter.SetHandRayColor(HandType.Right, ThemeColors.GetRayColor(shotType, rightFocusType));

            _playerControlPresenter.SetAimRayColor(HandType.Left, ThemeColors.GetRayColor(shotType, leftFocusType));
            _playerControlPresenter.SetAimRayColor(HandType.Right, ThemeColors.GetRayColor(shotType, rightFocusType));

            var nonDominantHand = _playerSettingDataStore.NonDominantHand.Value;
            _playerControlPresenter.SetAimEnableRay(nonDominantHand, shotType != ShotType.Merge);
            _playerControlPresenter.SetHandEnableRay(nonDominantHand, shotType != ShotType.Merge);
        }

        public void Tick()
        {
            if (_gameInputUseCase.IsLeftTrigger.Value)
            {
                TryLeftShot();
            }

            if (_gameInputUseCase.IsRightTrigger.Value)
            {
                TryRightShot();
            }
        }

        private void TryLeftShot()
        {
            var shotType = _playerDataStore.ShotType.Value;

            if (!_playerBulletParameterDataStore.CanShot(HandType.Left, shotType))
            {
                return;
            }

            var focusType = _playerDataStore.LeftFocusType.Value;
            var bulletData = _playerBulletParameterDataStore.GetBulletData(shotType, focusType);

            _playerBulletParameterDataStore.SetCoolDownTime(HandType.Left, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Left, bulletData, _playerDataStore.FocusLeftTargetId.Value);
        }

        private void TryRightShot()
        {
            var shotType = _playerDataStore.ShotType.Value;

            if (!_playerBulletParameterDataStore.CanShot(HandType.Right, shotType))
            {
                return;
            }

            var focusType = _playerDataStore.RightFocusType.Value;
            var bulletData = _playerBulletParameterDataStore.GetBulletData(shotType, focusType);

            _playerBulletParameterDataStore.SetCoolDownTime(HandType.Right, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Right, bulletData,
                _playerDataStore.FocusRightTargetId.Value);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}