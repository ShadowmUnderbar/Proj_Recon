namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ホップステップ（連続回避の回数上限を増やし、回避のクールダウンを短くする）
    /// </summary>
    public interface IHopStepsDataStore
    {
        /// <summary>
        /// 回避回数の上限に足す回数。所持中の最高レベルの Value1。未所持なら0
        /// </summary>
        int GetExtraDodgeCount();

        /// <summary>
        /// 回避のクールダウンに掛ける倍率。所持中の最高レベルの Value2。未所持なら1
        /// </summary>
        float GetCooldownMultiplier();
    }
}
