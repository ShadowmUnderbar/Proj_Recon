using System.Collections.Generic;
using App.Common.Data.MasterData;

namespace App.Common.Data
{
    /// <summary>
    /// 所持アップグレードのタグを件数の多い順に並べる計算。
    /// 同じアップグレード（NameKey）はレベル違いを複数持っていても1件と数え、同数のタグは enum の定義順に並べる
    /// </summary>
    public static class UpgradeTagRanking
    {
        public static IReadOnlyList<UpgradeTagCount> Top(IEnumerable<UpgradeMasterData> upgrades, int maxCount)
        {
            var result = new List<UpgradeTagCount>();
            if (upgrades == null || maxCount <= 0)
            {
                return result;
            }

            var countedNameKeys = new HashSet<string>();
            var counts = new Dictionary<UpgradeTag, int>();
            foreach (var upgrade in upgrades)
            {
                if (upgrade == null || !countedNameKeys.Add(upgrade.NameKey))
                {
                    continue;
                }

                foreach (var tag in upgrade.Tags)
                {
                    if (tag == UpgradeTag.None)
                    {
                        continue;
                    }

                    counts.TryGetValue(tag, out var count);
                    counts[tag] = count + 1;
                }
            }

            foreach (var pair in counts)
            {
                result.Add(new UpgradeTagCount(pair.Key, pair.Value));
            }

            result.Sort((a, b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : a.Tag.CompareTo(b.Tag));
            if (result.Count > maxCount)
            {
                result.RemoveRange(maxCount, result.Count - maxCount);
            }

            return result;
        }
    }
}
