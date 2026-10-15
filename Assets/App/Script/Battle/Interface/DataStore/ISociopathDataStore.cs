namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ソシオパス（敵を撃破するとレイジを得る。レイジ発動中なら倍の時間を得る。レイジ未所持でも発動する）
    /// </summary>
    public interface ISociopathDataStore
    {
        /// <summary>
        /// 敵を1体撃破したことを通知し、所持中の最高レベルの Value1 秒だけレイジを得る。
        /// すでにレイジが発動中なら Value1 × Value2 秒を得る。ソシオパス未所持なら何もしない
        /// </summary>
        /// <returns>得た秒数（得なかったときは0）</returns>
        float NotifyKill();
    }
}
