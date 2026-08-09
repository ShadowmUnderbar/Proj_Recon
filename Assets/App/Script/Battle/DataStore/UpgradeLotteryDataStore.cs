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
        private readonly IUpgradeEffectSimpleCalculatorDataStore _upgradeEffectSimpleCalculatorDataStore;

        [Inject]
        public UpgradeLotteryDataStore(
            UpgradeDatabase upgradeDatabase,
            IUpgradeSessionDataStore upgradeSessionDataStore,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IUpgradeEffectSimpleCalculatorDataStore upgradeEffectSimpleCalculatorDataStore
        )
        {
            _upgradeDatabase = upgradeDatabase;
            _upgradeSessionDataStore = upgradeSessionDataStore;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _upgradeEffectSimpleCalculatorDataStore = upgradeEffectSimpleCalculatorDataStore;
        }

        public IReadOnlyList<UpgradeMasterData> DrawUpgrades(int count)
        {
            // 出現可能 = 未取得 かつ 前提コアスキルがアンロック済み
            var candidates = _upgradeDatabase.UpgradeMasterData
                .Where(data => !_upgradeSessionDataStore.AppliedUpgrades.Contains(data.Id))
                .Where(IsUnlocked)
                .ToList();

            var drawCount = UnityEngine.Mathf.Min(count, candidates.Count);

            // 執着: 取得済みと同名（＝同系統の別レベル）の候補に追加の重みを与える。累積せず最高レベルのみ採用
            var fixationBonus = _upgradeEffectSimpleCalculatorDataStore.CalcMax(UpgradeType.Fixation);
            var ownedNameKeys = GetOwnedNameKeys();

            var weights = candidates
                .Select(data => ownedNameKeys.Contains(data.NameKey) ? 1f + fixationBonus : 1f)
                .ToList();

            var result = new List<UpgradeMasterData>(drawCount);
            for (var i = 0; i < drawCount; i++)
            {
                var pickedIndex = PickWeightedIndex(weights);
                result.Add(candidates[pickedIndex]);

                // 選ばれた候補を除外して次の抽選へ（重複なし）
                candidates.RemoveAt(pickedIndex);
                weights.RemoveAt(pickedIndex);
            }

            return result;
        }

        /// <summary>
        /// 重みに比例した確率でインデックスを1つ選ぶ。重みが全て0の場合は先頭を返す
        /// </summary>
        private static int PickWeightedIndex(IReadOnlyList<float> weights)
        {
            var totalWeight = 0f;
            foreach (var weight in weights)
            {
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return 0;
            }

            var value = UnityEngine.Random.Range(0f, totalWeight);
            for (var i = 0; i < weights.Count; i++)
            {
                value -= weights[i];
                if (value <= 0f)
                {
                    return i;
                }
            }

            // 浮動小数の誤差で末尾まで届かなかった場合の保険
            return weights.Count - 1;
        }

        /// <summary>
        /// 取得済みアップグレードのNameKey集合（執着の重み付け対象の判定に使う）
        /// </summary>
        private HashSet<string> GetOwnedNameKeys()
        {
            var nameKeys = new HashSet<string>();
            foreach (var id in _upgradeSessionDataStore.AppliedUpgrades)
            {
                if (_upgradeDatabase.TryGetUpgradeMasterData(id, out var data))
                {
                    nameKeys.Add(data.NameKey);
                }
            }

            return nameKeys;
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
