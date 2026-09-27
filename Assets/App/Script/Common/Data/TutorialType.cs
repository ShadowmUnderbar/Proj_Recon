namespace App.Common.Data
{
    /// <summary>
    /// チュートリアルの種類。閲覧回数はこの値ごとにセーブデータへ記録する。
    /// 文言は Localization の "TutorialText" テーブルから「$」＋種類名のキーで引く（例: Wave1 → $Wave1）。
    /// 値はセーブデータに数値で保存されるため、既存の番号は変えずに末尾へ追加すること。
    /// </summary>
    public enum TutorialType
    {
        /// <summary>移動・視点操作</summary>
        Movement = 0,

        /// <summary>射撃</summary>
        Shooting = 1,

        /// <summary>エイム（フォーカス）</summary>
        Aim = 2,

        /// <summary>回避</summary>
        Dodge = 3,

        /// <summary>ショップ（アップグレード購入）</summary>
        Shop = 4,

        /// <summary>ウェーブ1開始時の導入</summary>
        Wave1 = 5,
    }
}
