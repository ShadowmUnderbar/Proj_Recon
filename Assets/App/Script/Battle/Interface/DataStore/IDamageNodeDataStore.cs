namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ダメージ・ノードの効果計算。
    /// 有効な依存ノードの種類数に応じて攻撃力倍率が増減する（未所持時はデメリット倍率）。
    /// </summary>
    public interface IDamageNodeDataStore
    {
        /// <summary>弾ダメージに乗算する倍率。ダメージ・ノード未所持なら 1。</summary>
        float GetDamageMultiplier();
    }
}
