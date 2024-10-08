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
            _playerControlPresenter.OnFocusLeft
                .Subscribe(x => UpdateOnFocus(x,true))
                .AddTo(_disposables);

            _playerControlPresenter.OnFocusRight
                .Subscribe(x => UpdateOnFocus(x,false))
                .AddTo(_disposables);
        }

        private void UpdateOnFocus(int id,bool isLeft)
        {
            if(isLeft)
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