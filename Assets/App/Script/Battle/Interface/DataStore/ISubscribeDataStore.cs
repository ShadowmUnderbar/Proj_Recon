namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// サブスク（ウェーブクリア時に所持キャッシュを Value1 倍にする）
    /// </summary>
    public interface ISubscribeDataStore
    {
        /// <summary>
        /// ウェーブクリア時に所持キャッシュへ足すボーナス。所持キャッシュ × (Value1 - 1) の端数切り捨て。
        /// 所持中の最高レベルのみ採用。未所持なら0
        /// </summary>
        int GetWaveClearBonus(int currentPoint);
    }
}
