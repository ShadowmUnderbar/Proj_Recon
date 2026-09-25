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
    /// プレイヤーHPが0になったらゲームオーバーにし、ゲームオーバー画面を表示する。
    /// 画面のスロット保存ボタンで、そのランで獲得したアップグレードをメタ進行スロットへ保存する。
    /// 保存は何度でも行え、リスタートボタンでラン状態を初期化してビルド選択へ戻る。
    /// </summary>
    public class GameOverUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IGameStateDataStore _gameStateDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly IMetaProgressionDataStore _metaProgressionDataStore;
        private readonly IGameOverPresenter _gameOverPresenter;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly RunResetUseCase _runResetUseCase;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public GameOverUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IGameStateDataStore gameStateDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IMetaProgressionDataStore metaProgressionDataStore,
            IGameOverPresenter gameOverPresenter,
            IPlayerControlPresenter playerControlPresenter,
            RunResetUseCase runResetUseCase
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _gameStateDataStore = gameStateDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _metaProgressionDataStore = metaProgressionDataStore;
            _gameOverPresenter = gameOverPresenter;
            _playerControlPresenter = playerControlPresenter;
            _runResetUseCase = runResetUseCase;
        }

        public void Initialize()
        {
            // HPが0以下になったらゲームオーバー
            _playerStateDataStore.Health
                .Where(health => health <= 0f)
                .Subscribe(_ => OnPlayerDead())
                .AddTo(_disposable);

            // スロット保存ボタン（画面は開いたままなので、続けて別スロットへも保存できる）
            _gameOverPresenter.OnSaveSlotSelected
                .Subscribe(OnSaveSlotSelected)
                .AddTo(_disposable);

            // リスタートボタン
            _gameOverPresenter.OnRestart
                .Subscribe(_ => OnRestart())
                .AddTo(_disposable);
        }

        private void OnPlayerDead()
        {
            if (_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            _gameStateDataStore.SetGameOver();

            // ウェーブ進行・スポーンを停止（既存のポーズ機構を流用）
            _waveManagerDataStore.SetWavePause(true);

            // 保存対象はそのランで新たに獲得した分のみ（セット読込で最初から持っていた分は除外）
            var acquiredCount = _upgradeSessionDataStore.NewlyAcquiredUpgrades.Count;
            _gameOverPresenter.Show($"GAME OVER\n獲得アップグレード: {acquiredCount}個\nスロットに上書き保存してからリスタート");

            RefreshAllSlotLabels();

            // UI表示中だけボタン選択用のハンドレイを出す
            _playerControlPresenter.SetUiRayEnable(true);

            if (acquiredCount == 0)
            {
                _gameOverPresenter.SetStatus("このランで新たに獲得したアップグレードはありません");
            }
        }

        // スロットへ上書き保存する。画面は閉じないので、保存先を選び直してからリスタートできる
        private void OnSaveSlotSelected(int slotIndex)
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
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
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            _gameOverPresenter.Hide();
            _playerControlPresenter.SetUiRayEnable(false);

            // リセット後のビルド選択UIの表示・ハンドレイの再有効化はRunStartUseCaseが行う
            _runResetUseCase.ResetRun();
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
            _disposable.Dispose();
        }
    }
}
