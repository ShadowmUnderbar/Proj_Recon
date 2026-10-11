using App.Common.Data;

namespace App.Common.DataStore
{
    /// <summary>
    /// タグ名のローカライズキーの命名規則。
    /// キーは「$」＋<see cref="UpgradeTag"/>の名前（例: Barrier → $Barrier）
    /// </summary>
    internal static class TagLocalizationKey
    {
        private const string Prefix = "$";

        public static string Name(UpgradeTag tag) => Prefix + tag;
    }
}
