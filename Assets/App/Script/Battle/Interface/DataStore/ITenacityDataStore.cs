namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 執念（被弾すると受けたダメージの Value1 倍を即座に回復する。レイジ発動中なら Value2 倍）
    /// </summary>
    public interface ITenacityDataStore
    {
        /// <summary>
        /// 受けたダメージに対する回復量を返す。執念未所持なら0。
        /// レイジ発動中かは呼び出した時点で判定する（この被弾で得るレイジは含めないよう、被弾の通知より前に呼ぶ）
        /// </summary>
        float GetHealAmount(float damage);
    }
}
