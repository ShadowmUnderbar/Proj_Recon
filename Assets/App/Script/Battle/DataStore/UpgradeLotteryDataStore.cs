using System.Collections.Generic;
using System.Linq;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using App.Common.Interface;
using VContainer;

namespace App.Battle.DataStore
{
    public class UpgradeLotteryDataStore : IUpgradeLotteryDataStore
    {
        private readonly UpgradeDatabase _upgradeDatabase;
        private readonly IUpgradeSessionDataStore _upgradeSessionDataStore;
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;

        [Inject]
        public UpgradeLotteryDataStore(
            UpgradeDatabase upgradeDatabase,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore
        )
        {
            _upgradeDatabase = upgradeDatabase;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
        }

        public IReadOnlyList<UpgradeMasterData> DrawUpgrades(int count)
        {
            // 出現可能 = 未取得 かつ 前提コアスキルがアンロック済み
            var candidates = _upgradeDatabase.UpgradeMasterData
                .Where(data => !_upgradeSessionDataStore.AppliedUpgrades.Contains(data.Id))
                .Where(IsUnlocked)
                .ToList();

            // Fisher-Yates で先頭count件をランダム抽選（重複なし）
            var drawCount = UnityEngine.Mathf.Min(count, candidates.Count);
            for (var i = 0; i < drawCount; i++)
            {
                var swapIndex = UnityEngine.Random.Range(i, candidates.Count);
                (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
            }

            return candidates.GetRange(0, drawCount);
        }

        private bool IsUnlocked(UpgradeMasterData data)
        {
            return data.PlayerUnlockType switch
            {
                PlayerUnlockType.None => true,
                PlayerUnlockType.Akimbo => _coreSkillUnlockDataStore.IsUnLockAkimbo,
                PlayerUnlockType.Focus => _coreSkillUnlockDataStore.IsUnLockFocus,
                PlayerUnlockType.Waltz => _coreSkillUnlockDataStore.IsUnLockWaltz,
                PlayerUnlockType.Merge => _coreSkillUnlockDataStore.IsUnLockMerge,
                PlayerUnlockType.Blitz => _coreSkillUnlockDataStore.IsUnLockBlitz,
                _ => false,
            };
        }
    }
}
