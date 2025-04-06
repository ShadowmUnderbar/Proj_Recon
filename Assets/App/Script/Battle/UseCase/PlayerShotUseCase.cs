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
            _playerControlPresenter.SetRayColor(ShotTypeRayColors.GetRayColor(shotType));
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

            if (!_playerDataStore.CanShotCoolDown(true, shotType))
            {
                return;
            }

            switch (shotType)
            {
                case ShotType.Normal:
                    _playerDataStore.SetLeftNormalShotCoolDown(_playerDataStore.NormalFireRate);
                    break;
                case ShotType.Waltz:
                    _playerDataStore.SetLeftWaltzShotCoolDown(_playerDataStore.WaltzFireRate);
                    break;
                case ShotType.Merge:
                    _playerDataStore.SetMergeShotCoolDown(_playerDataStore.MergeFireRate);
                    break;
            }

            _playerControlPresenter.Shot(shotType, _playerDataStore.FocusLeftTargetId.Value, true);
        }

        private void TryRightShot()
        {
            var shotType = _playerDataStore.ShotType.Value;

            if (!_playerDataStore.CanShotCoolDown(false, shotType))
            {
                return;
            }

            switch (shotType)
            {
                case ShotType.Normal:
                    _playerDataStore.SetRightNormalShotCoolDown(_playerDataStore.NormalFireRate);
                    break;
                case ShotType.Waltz:
                    _playerDataStore.SetRightWaltzShotCoolDown(_playerDataStore.WaltzFireRate);
                    break;
                case ShotType.Merge:
                    _playerDataStore.SetMergeShotCoolDown(_playerDataStore.MergeFireRate);
                    break;
            }

            _playerControlPresenter.Shot(shotType, _playerDataStore.FocusRightTargetId.Value, false);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}