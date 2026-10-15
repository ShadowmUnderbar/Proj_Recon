namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ソシオパス（レイジ発動中に敵を撃破するとレイジの効果時間が延びる）
    /// </summary>
    public interface ISociopathDataStore
    {
        /// <summary>
        /// 敵を1体撃破したことを通知し、レイジが発動中なら所持中の最高レベルの Value1 秒だけ延長する。
        /// 未所持・レイジ非発動中は何もしない
        /// </summary>
        /// <returns>延長した秒数（延長しなかったときは0）</returns>
        float NotifyKill();
    }
}
