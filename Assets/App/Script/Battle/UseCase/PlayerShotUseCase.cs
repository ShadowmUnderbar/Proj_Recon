using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using R3;
using UnityEngine;

namespace App.Battle.UseCase
{
    public class PlayerShotUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerSettingDataStore _playerSettingDataStore;
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUsecase _gameInputUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerShotUseCase(
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerDataStore playerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUseCase
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _playerDataStore = playerDataStore;
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
            if (!_playerDataStore.CanShot(HandType.Left))
            {
                return;
            }

            var shotType = _playerDataStore.ShotType.Value;
            var focusType = _playerDataStore.LeftFocusType.Value;

            var bulletData = _playerDataStore.GetBulletData(shotType, focusType);

            _playerDataStore.SetCoolDownTime(HandType.Left, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Left, bulletData, _playerDataStore.FocusLeftTargetId.Value);
        }

        private void TryRightShot()
        {
            if (!_playerDataStore.CanShot(HandType.Right))
            {
                return;
            }

            var shotType = _playerDataStore.ShotType.Value;
            var focusType = _playerDataStore.RightFocusType.Value;

            var bulletData = _playerDataStore.GetBulletData(shotType, focusType);

            _playerDataStore.SetCoolDownTime(HandType.Right, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Right, bulletData,
                _playerDataStore.FocusRightTargetId.Value);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}