namespace App.Common.Data
{
    /// <summary>
    /// シーン名の定数。Build Settingsに登録した名前と一致させること。
    /// シーン名の直書きを一箇所に閉じ込めるために用意している。
    /// </summary>
    public static class SceneNames
    {
        /// <summary>起動シーン。共通の初期化を済ませてからメインメニューへ進む（Build Settings の先頭）</summary>
        public const string Boot = "Boot";

        /// <summary>タイトル・オプション・永続強化を扱うメインメニュー</summary>
        public const string MainMenu = "MainMenu";

        /// <summary>1回のランをプレイするバトルシーン</summary>
        public const string Battle = "Battle";
    }
}
