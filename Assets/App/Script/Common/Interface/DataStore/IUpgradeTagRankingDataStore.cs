using System.Collections.Generic;
using App.Common.Data;

namespace App.Common.Interface
{
    /// <summary>
    /// アップグレードIDの一覧から、タグを件数の多い順に集計する。
    /// バトルの所持アップグレード・メインメニューのセットなど、ID の一覧があればどこからでも使える
    /// </summary>
    public interface IUpgradeTagRankingDataStore
    {
        /// <summary>
        /// 件数の多い順に最大 maxCount 件のタグを返す。
        /// 同じアップグレードのレベル違いは1件と数え、同数のタグは enum の定義順に並べる
        /// </summary>
        IReadOnlyList<UpgradeTagCount> GetTopTags(IEnumerable<string> upgradeIds, int maxCount);
    }
}
