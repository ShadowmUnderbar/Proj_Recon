namespace App.Common.Data
{
    public enum UpgradeType
    {
        // 弾丸系
        BulletSpeedUp,      // 弾速アップ
        BulletDamageUp,     // ダメージアップ
        // 回避系
        DodgeDistanceUp,    // 回避距離アップ
        DodgeCountUp,       // 回避回数アップ
        DodgeCooldownDown,  // 回避クールダウン短縮
        // スキル系（解放済みスキルのみ候補に登場）
        FocusDamageUp,      // フォーカス弾強化
        BlitzCooldownDown,  // ブリッツクールダウン短縮
        WaltzFireRateUp,    // ワルツ連射強化
    }
}
