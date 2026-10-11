namespace App.Common.Data
{
    /// <summary>
    /// タグと、そのタグを持つ所持アップグレードの件数（同じアップグレードはレベル違いでも1件）
    /// </summary>
    public readonly struct UpgradeTagCount
    {
        public UpgradeTag Tag { get; }
        public int Count { get; }

        public UpgradeTagCount(UpgradeTag tag, int count)
        {
            Tag = tag;
            Count = count;
        }
    }
}
