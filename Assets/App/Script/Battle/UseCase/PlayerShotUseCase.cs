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
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerShotUseCase(
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerDataStore playerDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _playerDataStore = playerDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
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

            var dominantHand = _playerSettingDataStore.DominantHand.Value;
            var nonDominantHand = _playerSettingDataStore.NonDominantHand.Value;

            _playerControlPresenter.SetHandRayColor(dominantHand, ThemeColors.GetRayColor(shotType, rightFocusType));
            _playerControlPresenter.SetAimRayColor(dominantHand, ThemeColors.GetRayColor(shotType, rightFocusType));

            if (!_coreSkillUnlockDataStore.IsUnLockAkimbo)
            {
                _playerControlPresenter.SetAimEnableRay(nonDominantHand, false);
                _playerControlPresenter.SetHandEnableRay(nonDominantHand, false);
                return;
            }

            _playerControlPresenter.SetHandRayColor(nonDominantHand,
                ThemeColors.GetRayColor(shotType, leftFocusType));
            _playerControlPresenter.SetAimRayColor(nonDominantHand, ThemeColors.GetRayColor(shotType, leftFocusType));

            _playerControlPresenter.SetAimEnableRay(nonDominantHand, shotType != ShotType.Merge);
            _playerControlPresenter.SetHandEnableRay(nonDominantHand, shotType != ShotType.Merge);
        }

        public void Tick()
        {
            if (_gameInputDataStore.IsLeftTrigger.Value)
            {
                TryLeftShot();
            }

            if (_gameInputDataStore.IsRightTrigger.Value)
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