namespace App.Common.Data
{
    /// <summary>
    /// 表示用に名前を引いたタグと件数（ショップなどの「所持タグ上位」表示に渡す）
    /// </summary>
    public readonly struct LocalizedUpgradeTagCount
    {
        public string Name { get; }
        public int Count { get; }

        public LocalizedUpgradeTagCount(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }
}
