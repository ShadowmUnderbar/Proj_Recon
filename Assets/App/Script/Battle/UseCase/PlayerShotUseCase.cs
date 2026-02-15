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
        private readonly IPlayerShotTypeDataStore _playerShotTypeDataStore;
        private readonly IPlayerFocusDataStore _playerFocusDataStore;
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;
        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;
        private readonly IPlatformConfigDataStore _platformConfigDataStore;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerShotUseCase(
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerShotTypeDataStore playerShotTypeDataStore,
            IPlayerFocusDataStore playerFocusDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore,
            IPlatformConfigDataStore platformConfigDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _playerShotTypeDataStore = playerShotTypeDataStore;
            _playerFocusDataStore = playerFocusDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
            _platformConfigDataStore = platformConfigDataStore;
        }

        public void Initialize()
        {
            _playerShotTypeDataStore.ShotType
                .DistinctUntilChanged()
                .Subscribe(_ => OnUpdateShotType())
                .AddTo(_disposable);
            _playerFocusDataStore.RightFocusType
                .DistinctUntilChanged()
                .Subscribe(_ => OnUpdateShotType())
                .AddTo(_disposable);
            _playerFocusDataStore.LeftFocusType
                .DistinctUntilChanged()
                .Subscribe(_ => OnUpdateShotType())
                .AddTo(_disposable);
        }

        private void OnUpdateShotType()
        {
            var shotType = _playerShotTypeDataStore.ShotType.Value;
            var leftFocusType = _playerFocusDataStore.LeftFocusType.Value;
            var rightFocusType = _playerFocusDataStore.RightFocusType.Value;

            var dominantHand = _platformConfigDataStore.IsVRMode ? _playerSettingDataStore.DominantHand.Value : HandType.Left;
            var nonDominantHand = dominantHand == HandType.Right ? HandType.Left : HandType.Right;

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
            var shotType = _playerShotTypeDataStore.ShotType.Value;

            if (!_playerBulletParameterDataStore.CanShot(HandType.Left, shotType))
            {
                return;
            }

            var focusType = _playerFocusDataStore.LeftFocusType.Value;
            var bulletData = _playerBulletParameterDataStore.GetBulletData(shotType, focusType);

            _playerBulletParameterDataStore.SetCoolDownTime(HandType.Left, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Left, bulletData, _playerFocusDataStore.FocusLeftTargetId.Value);
        }

        private void TryRightShot()
        {
            var shotType = _playerShotTypeDataStore.ShotType.Value;

            if (!_playerBulletParameterDataStore.CanShot(HandType.Right, shotType))
            {
                return;
            }

            var focusType = _playerFocusDataStore.RightFocusType.Value;
            var bulletData = _playerBulletParameterDataStore.GetBulletData(shotType, focusType);

            _playerBulletParameterDataStore.SetCoolDownTime(HandType.Right, shotType, focusType);
            _playerControlPresenter.Shot(HandType.Right, bulletData,
                _playerFocusDataStore.FocusRightTargetId.Value);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
