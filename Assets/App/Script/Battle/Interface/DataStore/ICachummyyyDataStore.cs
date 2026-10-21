namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// CACHUMMYYY!（キャッシュ（ポイント粒子）を回収するたびに体力を回復する）
    /// </summary>
    public interface ICachummyyyDataStore
    {
        /// <summary>
        /// ポイント粒子を1個回収したときの回復量を返す。
        /// 粒子の値が Value1 以上なら Value2 ＋ 粒子の値 × Value3。所持中の最高レベルのみ採用。未所持・値が足りないなら0
        /// </summary>
        float GetHealAmount(int point);
    }
}
