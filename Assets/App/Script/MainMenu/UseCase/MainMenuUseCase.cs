using System;
using App.Common.Interface;
using App.MainMenu.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu.UseCase
{
    /// <summary>
    /// メインメニューの操作を受け付ける。現時点はSTARTボタンでバトルシーンへ遷移するだけ。
    /// オプション・永続強化の導線は後から増やす。
    /// </summary>
    public class MainMenuUseCase : IInitializable, IDisposable
    {
        private readonly IMainMenuPresenter _mainMenuPresenter;
        private readonly ISceneTransitionUseCase _sceneTransitionUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public MainMenuUseCase(
            IMainMenuPresenter mainMenuPresenter,
            ISceneTransitionUseCase sceneTransitionUseCase
        )
        {
            _mainMenuPresenter = mainMenuPresenter;
            _sceneTransitionUseCase = sceneTransitionUseCase;
        }

        public void Initialize()
        {
            _mainMenuPresenter.OnStart
                .Subscribe(_ => OnStart())
                .AddTo(_disposable);
        }

        private void OnStart()
        {
            if (_sceneTransitionUseCase.IsTransitioning)
            {
                return;
            }

            // 読み込み中に押し直せないようボタンを落としてから遷移する
            _mainMenuPresenter.SetInteractable(false);

            if (!_sceneTransitionUseCase.LoadBattle())
            {
                // 遷移を開始できなかったときにボタンを落としたままにすると
                // タイトルから進めなくなるため、押し直せる状態へ戻す
                _mainMenuPresenter.SetInteractable(true);
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
