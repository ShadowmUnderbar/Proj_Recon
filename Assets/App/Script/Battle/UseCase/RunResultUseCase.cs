using System;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ランの結果画面（ゲームオーバー・クリア共用）の操作。
    /// 画面のスロット保存ボタンで、そのランで獲得したアップグレードをメタ進行スロットへ保存する。
    /// 保存は何度でも行え、リスタートボタンでラン状態を初期化してビルド選択へ戻る。
    /// メインメニューボタンではシーンごと切り替えてタイトルへ戻る。
    /// いつ画面を出すか（死亡演出・クリア表示の後）は GameOverUseCase / GameClearUseCase が決める。
    /// </summary>
    public class RunResultUseCase : IInitializable, IDisposable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly IAcquiredUpgradeListBuilder _acquiredUpgradeListBuilder;
        private readonly IMetaProgressionDataStore _metaProgressionDataStore;
        private readonly IGameOverPresenter _gameOverPresenter;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly RunResetUseCase _runResetUseCase;
        private readonly ISceneTransitionUseCase _sceneTransitionUseCase;

        private readonly CompositeDisposable _disposable = new();

        private readonly Subject<Unit> _onClosing = new();

        /// <summary>
        /// 画面を閉じてリスタート・メインメニューへ移る直前に流れる。演出を畳むのに使う。
        /// リスタートではラン状態のリセットより先に流れる
        /// </summary>
        public Observable<Unit> OnClosing => _onClosing;

        // ボタン付きの結果画面を出しているか（見出しだけのクリア表示中は操作を受け付けない）
        private bool _isResultShown;

        [Inject]
        public RunResultUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IGameStateDataStore gameStateDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IAcquiredUpgradeListBuilder acquiredUpgradeListBuilder,
            IMetaProgressionDataStore metaProgressionDataStore,
            IGameOverPresenter gameOverPresenter,
            IPlayerControlPresenter playerControlPresenter,
            RunResetUseCase runResetUseCase,
            ISceneTransitionUseCase sceneTransitionUseCase
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _gameStateDataStore = gameStateDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _acquiredUpgradeListBuilder = acquiredUpgradeListBuilder;
            _metaProgressionDataStore = metaProgressionDataStore;
            _gameOverPresenter = gameOverPresenter;
            _playerControlPresenter = playerControlPresenter;
            _runResetUseCase = runResetUseCase;
            _sceneTransitionUseCase = sceneTransitionUseCase;
        }

        public void Initialize()
        {
            // スロット保存ボタン（画面は開いたままなので、続けて別スロットへも保存できる）
            _gameOverPresenter.OnSaveSlotSelected
                .Subscribe(OnSaveSlotSelected)
                .AddTo(_disposable);

            // リスタートボタン
            _gameOverPresenter.OnRestart
                .Subscribe(_ => OnRestart())
                .AddTo(_disposable);

            // メインメニューへ戻るボタン
            _gameOverPresenter.OnReturnToMainMenu
                .Subscribe(_ => OnReturnToMainMenu())
                .AddTo(_disposable);
        }

        /// <summary>ボタンを出さずに見出しだけを表示する（クリア直後の表示）</summary>
        public void ShowHeadlineOnly(string headline)
        {
            _isResultShown = false;
            _gameOverPresenter.ShowHeadlineOnly(headline);
        }

        /// <summary>結果画面をボタンごと表示する</summary>
        /// <param name="title">見出しの1行目（GAME OVER / STAGE CLEAR）</param>
        public void Show(string title)
        {
            // 保存対象はそのランで新たに獲得した分のみ（セット読込で最初から持っていた分は除外）
            var acquired = _upgradeSessionDataStore.NewlyAcquiredUpgrades;
            var acquiredCount = acquired.Count;
            _gameOverPresenter.Show($"{title}\n獲得アップグレード: {acquiredCount}個\nスロットに上書き保存してからリスタート");
            _gameOverPresenter.SetUpgradeList(_acquiredUpgradeListBuilder.Build(acquired));
            _isResultShown = true;

            RefreshAllSlotLabels();

            // UI表示中だけボタン選択用のハンドレイを出す
            _playerControlPresenter.SetUiRayEnable(true);

            if (acquiredCount == 0)
            {
                _gameOverPresenter.SetStatus("このランで新たに獲得したアップグレードはありません");
            }
        }

        private bool CanOperate => _isResultShown && _gameStateDataStore.IsRunEnded;

        // スロットへ上書き保存する。画面は閉じないので、保存先を選び直してからリスタートできる
        private void OnSaveSlotSelected(int slotIndex)
        {
            if (!CanOperate)
            {
                return;
            }

            var ids = _upgradeSessionDataStore.NewlyAcquiredUpgrades;
            var clearedWave = _waveManagerDataStore.CurrentWave.CurrentValue;

            _metaProgressionDataStore.SaveToSlot(slotIndex, ids, clearedWave);

            // 保存結果をラベル（現在: 〜）と状態テキストの両方に反映する
            RefreshAllSlotLabels();
            _gameOverPresenter.SetStatus($"スロット{slotIndex + 1}に保存しました（{ids.Count}個 / Wave{clearedWave}）");
        }

        // ラン状態を初期化してビルド選択からやり直す
        private void OnRestart()
        {
            if (!CanOperate)
            {
                return;
            }

            Close();

            // リセット後のビルド選択UIの表示・ハンドレイの再有効化はRunStartUseCaseが行う
            _runResetUseCase.ResetRun();
        }

        // メインメニューシーンへ戻る。ラン状態はシーンごと破棄されるためリセットは行わない
        private void OnReturnToMainMenu()
        {
            if (!CanOperate || _sceneTransitionUseCase.IsTransitioning)
            {
                return;
            }

            // 遷移を開始できてから画面を畳む。先に畳むと、遷移に失敗したときに
            // 保存もリスタートもできない状態で取り残される
            if (!_sceneTransitionUseCase.LoadMainMenu())
            {
                _gameOverPresenter.SetStatus("メインメニューへ移動できませんでした");
                return;
            }

            Close();
        }

        private void Close()
        {
            _isResultShown = false;
            _gameOverPresenter.Hide();
            _playerControlPresenter.SetUiRayEnable(false);

            // 倒れた姿勢のままランが始まらないよう、リセットの前に演出を畳ませる
            _onClosing.OnNext(Unit.Default);
        }

        private void RefreshAllSlotLabels()
        {
            for (var i = 0; i < _metaProgressionDataStore.SlotCount; i++)
            {
                _gameOverPresenter.SetSlotLabel(i, BuildSlotLabel(i));
            }
        }

        private string BuildSlotLabel(int slotIndex)
        {
            if (_metaProgressionDataStore.IsSlotEmpty(slotIndex))
            {
                return $"スロット{slotIndex + 1}に保存\n(現在: 空)";
            }

            var count = _metaProgressionDataStore.GetSlotUpgradeIds(slotIndex).Count;
            var wave = _metaProgressionDataStore.GetSlotClearedWave(slotIndex);
            return $"スロット{slotIndex + 1}に保存\n(現在: {count}個 / Wave{wave})";
        }

        public void Dispose()
        {
            _onClosing.Dispose();
            _disposable.Dispose();
        }
    }
}
