using System;
using App.Common.Interface;
using App.MainMenu.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.MainMenu.UseCase
{
    /// <summary>
    /// メインメニューからバトルを開始する導線。
    /// STARTを押すと保存済みアップグレードセットの選択（RunStartView）に切り替わり、
    /// スロットを選ぶか「使わずに開始」で、その選択を <see cref="IRunLoadoutDataStore"/> に積んでバトルシーンへ遷移する。
    /// 選んだセットの装備はバトル側の RunStartUseCase が行う。
    /// </summary>
    public class MainMenuUseCase : IInitializable, IDisposable
    {
        private readonly IMainMenuPresenter _mainMenuPresenter;
        private readonly IRunStartPresenter _runStartPresenter;
        private readonly IRunLoadoutDataStore _runLoadoutDataStore;
        private readonly IMetaProgressionDataStore _metaProgressionDataStore;
        private readonly ISceneTransitionUseCase _sceneTransitionUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public MainMenuUseCase(
            IMainMenuPresenter mainMenuPresenter,
            IRunStartPresenter runStartPresenter,
            IRunLoadoutDataStore runLoadoutDataStore,
            IMetaProgressionDataStore metaProgressionDataStore,
            ISceneTransitionUseCase sceneTransitionUseCase
        )
        {
            _mainMenuPresenter = mainMenuPresenter;
            _runStartPresenter = runStartPresenter;
            _runLoadoutDataStore = runLoadoutDataStore;
            _metaProgressionDataStore = metaProgressionDataStore;
            _sceneTransitionUseCase = sceneTransitionUseCase;
        }

        public void Initialize()
        {
            // 前回のランの選択を持ち越さない。バトルへ進むときに必ず選び直す
            _runLoadoutDataStore.Clear();

            _mainMenuPresenter.OnStart
                .Subscribe(_ => OnStart())
                .AddTo(_disposable);

            _runStartPresenter.OnSlotSelected
                .Subscribe(OnSlotSelected)
                .AddTo(_disposable);

            _runStartPresenter.OnStartWithoutLoad
                .Subscribe(_ => OnStartWithoutLoad())
                .AddTo(_disposable);

            _runStartPresenter.OnBack
                .Subscribe(_ => ShowTitle())
                .AddTo(_disposable);
        }

        /// <summary>START押下。タイトル表示からセット選択へ切り替える</summary>
        private void OnStart()
        {
            if (_sceneTransitionUseCase.IsTransitioning)
            {
                return;
            }

            _mainMenuPresenter.SetStartVisible(false);

            _runStartPresenter.Show("セット選択\nスロットを選ぶと最初から装備で開始 / 使わずに開始も可");
            RefreshAllSlotLabels();
        }

        /// <summary>セット選択からタイトル表示へ戻す</summary>
        private void ShowTitle()
        {
            _runStartPresenter.Hide();
            _mainMenuPresenter.SetStartVisible(true);
            _mainMenuPresenter.SetInteractable(true);
        }

        private void OnSlotSelected(int slotIndex)
        {
            // 空スロットは読み込まない（View側でも押下不可だが二重に防ぐ）
            if (_metaProgressionDataStore.IsSlotEmpty(slotIndex))
            {
                _runLoadoutDataStore.SelectNone();
            }
            else
            {
                _runLoadoutDataStore.Select(_metaProgressionDataStore.GetSlotUpgradeIds(slotIndex));
            }

            LoadBattle();
        }

        private void OnStartWithoutLoad()
        {
            _runLoadoutDataStore.SelectNone();
            LoadBattle();
        }

        private void LoadBattle()
        {
            if (_sceneTransitionUseCase.IsTransitioning)
            {
                return;
            }

            // 読み込み中に押し直せないよう選択画面を閉じてから遷移する
            _runStartPresenter.Hide();
            _mainMenuPresenter.SetInteractable(false);

            if (!_sceneTransitionUseCase.LoadBattle())
            {
                // 遷移を開始できなかったときに画面を閉じたままにすると
                // タイトルから進めなくなるため、選び直せる状態へ戻す
                _runLoadoutDataStore.Clear();
                ShowTitle();
            }
        }

        private void RefreshAllSlotLabels()
        {
            for (var i = 0; i < _metaProgressionDataStore.SlotCount; i++)
            {
                var empty = _metaProgressionDataStore.IsSlotEmpty(i);
                _runStartPresenter.SetSlot(i, BuildSlotLabel(i, empty), !empty);
            }
        }

        private string BuildSlotLabel(int slotIndex, bool empty)
        {
            if (empty)
            {
                return $"スロット{slotIndex + 1}\n(空)";
            }

            var count = _metaProgressionDataStore.GetSlotUpgradeIds(slotIndex).Count;
            var wave = _metaProgressionDataStore.GetSlotClearedWave(slotIndex);
            return $"スロット{slotIndex + 1}\n{count}個 / Wave{wave}";
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
