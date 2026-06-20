using App.Battle.Interface;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using R3;
using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;

namespace App.Battle.UseCase
{
    public class PlayerAimUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerAimDataStore _playerAimDataStore;
        private readonly IPlayerFocusDataStore _playerFocusDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputDataStore _gameInputDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerAimUseCase(
            IPlayerAimDataStore playerAimDataStore,
            IPlayerFocusDataStore playerFocusDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputDataStore gameInputDataStore
        )
        {
            _playerAimDataStore = playerAimDataStore;
            _playerFocusDataStore = playerFocusDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputDataStore = gameInputDataStore;
        }

        public void Initialize()
        {
            _playerControlPresenter.OnFocusLeft
                .Subscribe(x => UpdateOnFocus(x, true))
                .AddTo(_disposables);
            _playerControlPresenter.OnFocusRight
                .Subscribe(x => UpdateOnFocus(x, false))
                .AddTo(_disposables);

            _playerControlPresenter.OnLeftAimPosition
                .Subscribe(x => _playerAimDataStore.SetAimPosition(HandType.Left, x))
                .AddTo(_disposables);
            _playerControlPresenter.OnRightAimPosition
                .Subscribe(x => _playerAimDataStore.SetAimPosition(HandType.Right, x))
                .AddTo(_disposables);

            _playerControlPresenter.LeftHandPose
                .Subscribe(x => _playerAimDataStore.LeftHandPose.Value = x)
                .AddTo(_disposables);
            _playerControlPresenter.RightHandPose
                .Subscribe(x => _playerAimDataStore.RightHandPose.Value = x)
                .AddTo(_disposables);

            _gameInputDataStore.IsFocusLeft
                .Subscribe(x => _playerControlPresenter.IsFocusLeft(x))
                .AddTo(_disposables);

            _gameInputDataStore.IsFocusRight
                .Subscribe(x => _playerControlPresenter.IsFocusRight(x))
                .AddTo(_disposables);
        }

        private void UpdateOnFocus(int id, bool isLeft)
        {
            if (isLeft)
            {
                _playerFocusDataStore.FocusLeftTargetId.Value = id;
                return;
            }

            _playerFocusDataStore.FocusRightTargetId.Value = id;
        }

        public void Tick()
        {
            if (!DebugConfig.IsVRMode)
            {
                _playerControlPresenter.MouseAim(_gameInputDataStore.MouseInputPosition);
            }

            _playerControlPresenter.Aim();

            // 両手エイムの中心方向へモデルを振り向かせる
            _playerControlPresenter.SetModelFacing(_playerAimDataStore.CenterAimDirection);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
