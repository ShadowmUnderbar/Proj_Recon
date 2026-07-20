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

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public GameOverUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IWaveManagerDataStore waveManagerDataStore,
            IGameStateDataStore gameStateDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            IMetaProgressionDataStore metaProgressionDataStore,
            IGameOverPresenter gameOverPresenter
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _waveManagerDataStore = waveManagerDataStore;
            _gameStateDataStore = gameStateDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _metaProgressionDataStore = metaProgressionDataStore;
            _gameOverPresenter = gameOverPresenter;
        }

        public void Initialize()
        {
            // HPが0以下になったらゲームオーバー
            _playerStateDataStore.Health
                .Where(health => health <= 0f)
                .Subscribe(_ => OnPlayerDead())
                .AddTo(_disposable);

            // スロット保存ボタン
            _gameOverPresenter.OnSaveSlotSelected
                .Subscribe(OnSaveSlotSelected)
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

            // Phase1ではセット読込が未実装のため、獲得済み＝そのランで新規獲得したアップグレード。
            // （Phase2でセット読込を入れる際に「読込分を除外した新規獲得のみ」に絞る）
            var acquiredCount = _upgradeSessionDataStore.AppliedUpgrades.Count;
            _gameOverPresenter.Show($"GAME OVER\n獲得アップグレード: {acquiredCount}個\nスロットに保存できます");

            RefreshAllSlotLabels();

            if (acquiredCount == 0)
            {
                _gameOverPresenter.SetStatus("このランで新たに獲得したアップグレードはありません");
            }
        }

        private void OnSaveSlotSelected(int slotIndex)
        {
            if (!_gameStateDataStore.IsGameOver.CurrentValue)
            {
                return;
            }

            var ids = _upgradeSessionDataStore.AppliedUpgrades;
            var clearedWave = _waveManagerDataStore.CurrentWave.CurrentValue;

            _metaProgressionDataStore.SaveToSlot(slotIndex, ids, clearedWave);

            _gameOverPresenter.SetStatus($"スロット{slotIndex + 1}に {ids.Count}個 を保存しました");
            _gameOverPresenter.SetSlotLabel(slotIndex, BuildSlotLabel(slotIndex));
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
