namespace App.Battle.Data
{
    /// <summary>
    /// プレイヤーに関する固定値。バランス調整の対象ではないものだけを置く
    /// （調整対象は PlayerBaseParameterConfig）。
    /// </summary>
    public static class PlayerConstants
    {
        /// <summary>弾の攻撃者IDとして使うプレイヤーのID。敵IDと衝突しない値</summary>
        public const int PlayerId = 1;
    }
}
