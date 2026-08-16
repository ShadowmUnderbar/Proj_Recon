using System;
using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data.MasterData;
using R3;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeSessionDataStore : IUpgradeSessionDataStore, IDisposable
    {
        // 所持している全アップグレード（読込分＋新規獲得分）
        private readonly List<string> _appliedUpgrades = new();

        // セット読込で最初から付与された分（新規獲得＝保存対象から除外するために記録）
        private readonly HashSet<string> _preloadedIds = new();

        public IReadOnlyList<string> AppliedUpgrades => _appliedUpgrades;

        private readonly Subject<Unit> _onChanged = new();
        public Observable<Unit> OnChanged => _onChanged;

        public IReadOnlyList<string> NewlyAcquiredUpgrades =>
            _appliedUpgrades.Where(id => !_preloadedIds.Contains(id)).ToList();

        [Inject]
        public UpgradeSessionDataStore() { }

        public void AddUpgrade(UpgradeMasterData upgradeData)
        {
            if (_appliedUpgrades.Contains(upgradeData.Id))
            {
                return;
            }

            _appliedUpgrades.Add(upgradeData.Id);
            _onChanged.OnNext(Unit.Default);
        }

        public void Preload(UpgradeMasterData upgradeData)
        {
            if (_appliedUpgrades.Contains(upgradeData.Id))
            {
                return;
            }

            _appliedUpgrades.Add(upgradeData.Id);
            _preloadedIds.Add(upgradeData.Id);
            _onChanged.OnNext(Unit.Default);
        }

        public void Reset()
        {
            _appliedUpgrades.Clear();
            _preloadedIds.Clear();
            _onChanged.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _onChanged.Dispose();
        }
    }
}
