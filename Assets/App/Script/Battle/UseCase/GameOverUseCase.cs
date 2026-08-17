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

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public GameOverUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IGameStateDataStore gameStateDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IMetaProgressionDataStore metaProgressionDataStore,
            IGameOverPresenter gameOverPresenter,
            IPlayerControlPresenter playerControlPresenter
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _gameStateDataStore = gameStateDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _metaProgressionDataStore = metaProgressionDataStore;
            _gameOverPresenter = gameOverPresenter;
            _playerControlPresenter = playerControlPresenter;
        }

        public void Initialize()
        {
            // HPが0以下になったらゲームオーバー
            _playerStateDataStore.Health
                .Where(health => health <= 0f)
                .Subscribe(_ => OnPlayerDead())
                .AddTo(_disposable);

            // スロット保存ボタン（保存して終了）
            _gameOverPresenter.OnSaveSlotSelected
                .Subscribe(OnSaveSlotSelected)
                .AddTo(_disposable);

            // セーブせずに終了ボタン
            _gameOverPresenter.OnExitWithoutSave
                .Subscribe(_ => OnExitWithoutSave())
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
            _gameOverPresenter.Show($"GAME OVER\n獲得アップグレード: {acquiredCount}個\nスロットに上書き保存、またはセーブせずに終了");

            RefreshAllSlotLabels();

            // UI表示中だけボタン選択用のハンドレイを出す
            _playerControlPresenter.SetUiRayEnable(true);

            if (acquiredCount == 0)
            {
                _gameOverPresenter.SetStatus("このランで新たに獲得したアップグレードはありません");
            }
        }

        // スロットへ上書き保存して終了する
        private void OnSaveSlotSelected(int slotIndex)
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            var ids = _upgradeSessionDataStore.NewlyAcquiredUpgrades;
            var clearedWave = _waveManagerDataStore.CurrentWave.CurrentValue;

            _metaProgressionDataStore.SaveToSlot(slotIndex, ids, clearedWave);

            // 保存もセーブせず終了も同じ終了フロー（画面を閉じる）。
            // 閉じた後のリスタート等はPhase2で対応する
            CloseGameOver();
        }

        // セーブせずに終了する
        private void OnExitWithoutSave()
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            CloseGameOver();
        }

        private void CloseGameOver()
        {
            _gameOverPresenter.Hide();
            _playerControlPresenter.SetUiRayEnable(false);
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
