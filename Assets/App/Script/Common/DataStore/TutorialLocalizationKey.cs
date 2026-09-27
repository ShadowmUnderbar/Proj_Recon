using App.Common.Data;

namespace App.Common.DataStore
{
    /// <summary>
    /// チュートリアル用ローカライズキーの命名規則。
    /// キーは「$」＋<see cref="TutorialType"/>の名前（例: Wave1 → $Wave1）
    /// </summary>
    internal static class TutorialLocalizationKey
    {
        private const string Prefix = "$";

        public static string Text(TutorialType type) => Prefix + type;
    }
}
