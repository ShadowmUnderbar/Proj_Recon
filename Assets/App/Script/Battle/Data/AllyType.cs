namespace App.Battle.Data
{
    /// <summary>
    /// 召喚する味方の種類。種類ごとのパラメータは <see cref="AllyConfig"/>、プレハブは AllyStoreView が持つ
    /// </summary>
    public enum AllyType
    {
        None = 0,

        /// <summary>その場に据え置き、周囲の敵を自動で撃つ（サモンタレット）</summary>
        Turret = 1,

        /// <summary>プレイヤーの後ろ上方に浮かんで追従し、周囲の敵を自動で撃つ（サモンドローン）</summary>
        Drone = 2,
    }
}
