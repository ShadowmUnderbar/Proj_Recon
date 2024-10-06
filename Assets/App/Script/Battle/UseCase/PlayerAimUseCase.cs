using App.Battle.Interface;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;
using App.Battle.DataStore;
using R3;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Battle.UseCase
{
    public class PlayerAimUseCase : IPlayerAimUseCase, IInitializable, ITickable ,IDisposable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUsecase _gameInputUsecase;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerAimUseCase(
            IPlayerDataStore playerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUsecase
        )
        {
            _playerDataStore = playerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _gameInputUsecase = gameInputUsecase;
        }

        public void Initialize()
        {
            _playerControlPresenter.OnFocus
                .Subscribe(OnFocus)
                .AddTo(_disposables);

            _playerControlPresenter.OnUnFocus
                .Subscribe(OnUnFocus)
                .AddTo(_disposables);
        }

        private void OnFocus(int id)
        {
            if(id == 0)
            {
                _playerDataStore.IsFocusLeft.Value = true;
                return;
            }

            _playerDataStore.IsFocusRight.Value = true;
        }

        private void OnUnFocus(int id)
        {
            if (id == 0)
            {
                _playerDataStore.IsFocusLeft.Value = false;
                return;
            }

            _playerDataStore.IsFocusRight.Value = false;
        }

        public void Tick()
        {
#if UNITY_EDITOR
            if (!EditorPrefs.GetBool("VRMode", false))
            {
                _playerControlPresenter.MouseAim(_gameInputUsecase.MouseInputPosition);
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