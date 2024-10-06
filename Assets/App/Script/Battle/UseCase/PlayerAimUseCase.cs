using App.Battle.Interface;
using App.Common.UseCase;
using VContainer;
using VContainer.Unity;
using App.Common.Interface;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace App.Battle.UseCase
{
    public class PlayerAimUseCase : IPlayerAimUseCase, ITickable
    {
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IGameInputUsecase _gameInputUsecase;

        [Inject]
        public PlayerAimUseCase(
            IPlayerControlPresenter playerControlPresenter,
            IGameInputUsecase gameInputUsecase
        )
        {
            _playerControlPresenter = playerControlPresenter;
            _gameInputUsecase = gameInputUsecase;
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
    }
}