using System;
using App.Common.Interface;
using App.MainMenu.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu.UseCase
{
    /// <summary>
    /// メインメニューの操作を受け付ける。STARTでバトルシーンへ遷移し、
    /// OPTIONでメインパネルとオプションパネルを切り替える。
    /// オプションの各設定値の反映は<see cref="OptionUseCase"/>が担当し、
    /// ここでは「どちらのパネルを見せるか」だけを扱う。
    /// 永続強化の導線は後から増やす。
    /// </summary>
    public class MainMenuUseCase : IInitializable, IDisposable
    {
        private readonly IMainMenuPresenter _mainMenuPresenter;
        private readonly IOptionPanelPresenter _optionPanelPresenter;
        private readonly ISceneTransitionUseCase _sceneTransitionUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public MainMenuUseCase(
            IMainMenuPresenter mainMenuPresenter,
            IOptionPanelPresenter optionPanelPresenter,
            ISceneTransitionUseCase sceneTransitionUseCase
        )
        {
            _mainMenuPresenter = mainMenuPresenter;
            _optionPanelPresenter = optionPanelPresenter;
            _sceneTransitionUseCase = sceneTransitionUseCase;
        }

        public void Initialize()
        {
            _mainMenuPresenter.OnStart
                .Subscribe(_ => OnStart())
                .AddTo(_disposable);
            _mainMenuPresenter.OnOption
                .Subscribe(_ => OnOption())
                .AddTo(_disposable);
            _optionPanelPresenter.OnClose
                .Subscribe(_ => OnOptionClose())
                .AddTo(_disposable);

            // シーンの保存状態に依らず、開始時は必ずメインパネルから見せる
            _mainMenuPresenter.ShowMainPanel();
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

        private void OnOption()
        {
            if (_sceneTransitionUseCase.IsTransitioning)
            {
                return;
            }

            _mainMenuPresenter.ShowOptionPanel();
        }

        private void OnOptionClose()
        {
            _mainMenuPresenter.ShowMainPanel();
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
