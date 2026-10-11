namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ブルズアイ。1発の弾が敵を貫通するごとに、以降の敵へのダメージ倍率が上がる。
    /// </summary>
    public interface IBullseyeDataStore
    {
        /// <summary>
        /// 同一弾内で何体目のヒットかに応じたダメージ倍率を返す。未所持・1体目は 1.0f
        /// </summary>
        /// <param name="penetrationIndex">同一弾内で何体目のヒットか（1始まり）</param>
        float GetDamageMultiplier(int penetrationIndex);
    }
}
