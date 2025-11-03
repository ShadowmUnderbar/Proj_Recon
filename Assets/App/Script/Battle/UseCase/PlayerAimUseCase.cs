using App.Battle.Interface;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using R3;
using System;
using App.Battle.Interface.DataStore;
using App.Common.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Battle.UseCase
{
    public class PlayerAimUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUseCase _gameInputUseCase;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerAimUseCase(
            IPlayerDataStore playerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUseCase gameInputUseCase
        )
        {
            _playerDataStore = playerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputUseCase = gameInputUseCase;
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
                .Subscribe(x => _playerDataStore.SetAimPosition(HandType.Left, x))
                .AddTo(_disposables);
            _playerControlPresenter.OnRightAimPosition
                .Subscribe(x => _playerDataStore.SetAimPosition(HandType.Right, x))
                .AddTo(_disposables);

            _playerControlPresenter.LeftHandPose
                .Subscribe(x => _playerDataStore.LeftHandPose.Value = x)
                .AddTo(_disposables);
            _playerControlPresenter.RightHandPose
                .Subscribe(x => _playerDataStore.RightHandPose.Value = x)
                .AddTo(_disposables);

            _gameInputUseCase.IsFocusLeft
                .Subscribe(x => _playerControlPresenter.IsFocusLeft(x))
                .AddTo(_disposables);

            _gameInputUseCase.IsFocusRight
                .Subscribe(x => _playerControlPresenter.IsFocusRight(x))
                .AddTo(_disposables);
        }

        private void UpdateOnFocus(int id, bool isLeft)
        {
            if (isLeft)
            {
                _playerDataStore.FocusLeftTargetId.Value = id;
                return;
            }

            _playerDataStore.FocusRightTargetId.Value = id;
        }

        public void Tick()
        {
#if UNITY_EDITOR
            if (!EditorPrefs.GetBool("VRMode", false))
            {
                _playerControlPresenter.MouseAim(_gameInputUseCase.MouseInputPosition);
            }
#endif
            _playerControlPresenter.Aim();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}