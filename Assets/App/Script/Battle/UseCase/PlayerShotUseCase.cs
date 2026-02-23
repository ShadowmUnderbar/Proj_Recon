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

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public PlayerShotUseCase(
            IPlayerSettingDataStore playerSettingDataStore,
            IPlayerShotTypeDataStore playerShotTypeDataStore,
            IPlayerFocusDataStore playerFocusDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore
        )
        {
            _playerSettingDataStore = playerSettingDataStore;
            _playerShotTypeDataStore = playerShotTypeDataStore;
            _playerFocusDataStore = playerFocusDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
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

            var dominantHand = DebugConfig.IsVRMode ? _playerSettingDataStore.DominantHand.Value : HandType.Left;
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
                TryShot(HandType.Left);
            }

            if (_gameInputDataStore.IsRightTrigger.Value)
            {
                TryShot(HandType.Right);
            }
        }

        private void TryShot(HandType handType)
        {
            var shotType = _playerShotTypeDataStore.ShotType.Value;

            if (!_playerBulletParameterDataStore.CanShot(handType, shotType))
            {
                return;
            }

            var focusType = handType == HandType.Left
                ? _playerFocusDataStore.LeftFocusType.Value
                : _playerFocusDataStore.RightFocusType.Value;

            var bulletData = _playerBulletParameterDataStore.GetBulletData(shotType, focusType);

            var targetId = handType == HandType.Left
                ? _playerFocusDataStore.FocusLeftTargetId.Value
                : _playerFocusDataStore.FocusRightTargetId.Value;

            _playerBulletParameterDataStore.SetCoolDownTime(handType, shotType, focusType);
            _playerControlPresenter.Shot(handType, bulletData, targetId);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}