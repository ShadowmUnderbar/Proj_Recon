namespace App.Battle.Data
{
    /// <summary>
    /// アップグレード1件分のローカライズ済み文言。
    /// 表示側はこの値をそのまま使う（キー解決・値の埋め込みは DataStore 側で済ませる）
    /// </summary>
    public readonly struct UpgradeLocalizedText
    {
        /// <summary>タイトル（例: $BaseDamageUp →「基礎威力アップ」）</summary>
        public string Title { get; }

        /// <summary>簡略説明（例: $BaseDamageUp_SimpleDesc →「攻撃力が上昇する」）</summary>
        public string SimpleDescription { get; }

        /// <summary>
        /// 詳細説明。倍率系タイプは {value1} を増減率で埋め込み済み（例:「攻撃力の基礎値が10%アップ」）。
        /// それ以外のタイプは変換規則が未定のため {valueN} のまま
        /// </summary>
        public string Description { get; }

        /// <summary>レベル表記（例: $Level1_Upgrade →「-レベル1」）</summary>
        public string LevelLabel { get; }

        public UpgradeLocalizedText(string title, string simpleDescription, string description, string levelLabel)
        {
            Title = title;
            SimpleDescription = simpleDescription;
            Description = description;
            LevelLabel = levelLabel;
        }
    }
}
