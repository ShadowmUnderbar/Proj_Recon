namespace App.Battle.DataStore
{
    /// <summary>
    /// アップグレード用ローカライズキーの命名規則。
    /// タイトルは NameKey そのもの（例: $BaseDamageUp）、説明はその接尾辞付き、レベルは共通キー
    /// </summary>
    internal static class UpgradeLocalizationKey
    {
        private const string SimpleDescriptionSuffix = "_SimpleDesc";
        private const string DescriptionSuffix = "_Desc";
        private const string LevelKeyFormat = "$Level{0}_Upgrade";

        public static string Title(string nameKey) => nameKey;

        public static string SimpleDescription(string nameKey) => nameKey + SimpleDescriptionSuffix;

        public static string Description(string nameKey) => nameKey + DescriptionSuffix;

        public static string Level(int level) => string.Format(LevelKeyFormat, level);
    }
}
