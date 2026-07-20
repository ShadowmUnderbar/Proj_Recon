using System;
using System.Collections.Generic;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using App.Common.Interface;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// ラン開始時にセット選択UIを出し、選択されたスロットのアップグレードを最初から装備してランを開始する。
    /// 選択中はゲームを停止（IsWavePause=true）し、選択完了で解除してウェーブ1を開始する。
    /// </summary>
    public class RunStartUseCase : IInitializable, IDisposable
    {
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IRunStartDataStore _runStartDataStore;
        private readonly IMetaProgressionDataStore _metaProgressionDataStore;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly UpgradeSideEffectApplier _upgradeSideEffectApplier;
        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly IRunStartPresenter _runStartPresenter;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public RunStartUseCase(
            IWaveManagerDataStore waveManagerDataStore,
            IRunStartDataStore runStartDataStore,
            IMetaProgressionDataStore metaProgressionDataStore,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            UpgradeSideEffectApplier upgradeSideEffectApplier,
            UpgradeDatabase upgradeDatabase,
            IRunStartPresenter runStartPresenter
        )
        {
            _waveManagerDataStore = waveManagerDataStore;
            _runStartDataStore = runStartDataStore;
            _metaProgressionDataStore = metaProgressionDataStore;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _upgradeSideEffectApplier = upgradeSideEffectApplier;
            _upgradeDatabase = upgradeDatabase;
            _runStartPresenter = runStartPresenter;
        }

        public void Initialize()
        {
            _runStartPresenter.OnSlotSelected
                .Subscribe(OnSlotSelected)
                .AddTo(_disposable);

            _runStartPresenter.OnStartWithoutLoad
                .Subscribe(_ => StartRun())
                .AddTo(_disposable);

            // ラン開始時はゲームを止めてセット選択を待つ
            _runStartDataStore.SetSelecting(true);
            _waveManagerDataStore.SetWavePause(true);

            _runStartPresenter.Show("セット選択\nスロットを選ぶと最初から装備で開始 / 使わずに開始も可");
            RefreshAllSlotLabels();
        }

        private void OnSlotSelected(int slotIndex)
        {
            if (!_runStartDataStore.IsSelecting.CurrentValue)
            {
                return;
            }

            // 空スロットは読み込まない（View側でも押下不可だが二重に防ぐ）
            if (_metaProgressionDataStore.IsSlotEmpty(slotIndex))
            {
                StartRun();
                return;
            }

            // スロットのIDを解決し、まず全てPreload（AppliedUpgradesを揃えてから副作用を適用する）
            var upgrades = new List<UpgradeMasterData>();
            foreach (var id in _metaProgressionDataStore.GetSlotUpgradeIds(slotIndex))
            {
                if (_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                {
                    upgrades.Add(data);
                }
            }

            foreach (var upgrade in upgrades)
            {
                _upgradeSessionDataStore.Preload(upgrade);
            }

            // 付与副作用（GrantBuff起動・バリア満タン付与）はPreload後にまとめて適用する
            foreach (var upgrade in upgrades)
            {
                _upgradeSideEffectApplier.Apply(upgrade);
            }

            StartRun();
        }

        private void StartRun()
        {
            _runStartPresenter.Hide();
            _runStartDataStore.SetSelecting(false);

            // ポーズ解除でウェーブ1を開始（時間計測・スポーンが始動）
            _waveManagerDataStore.SetWavePause(false);
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
