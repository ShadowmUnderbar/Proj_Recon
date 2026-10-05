namespace App.Common.Data
{
    /// <summary>
    /// チュートリアルの種類。閲覧回数はこの値ごとにセーブデータへ記録する。
    /// 文言は Localization の "TutorialText" テーブルから「$」＋種類名のキーで引く（例: Wave1 → $Wave1）。
    /// 値はセーブデータに数値で保存されるため、既存の番号は変えずに末尾へ追加すること。
    /// </summary>
    public enum TutorialType
    {
        /// <summary>ウェーブ1開始時の導入</summary>
        Wave1 = 1,
        Wave2 = 2,
        Wave3 = 3,
        Wave4 = 4,
        Shop = 5,
        GameOver = 6,
        SelectSlot = 7,
        OtherBuild = 8
    }
}
