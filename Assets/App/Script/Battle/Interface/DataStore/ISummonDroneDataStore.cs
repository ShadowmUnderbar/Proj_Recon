namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// サモンドローン（敵を一定数撃破するたびに味方ドローンを召喚する）の実行時状態
    /// </summary>
    public interface ISummonDroneDataStore
    {
        /// <summary>次の召喚に向けて数えている撃破数</summary>
        int KillCount { get; }

        /// <summary>
        /// 敵を1体撃破したことを数え、召喚する数を返す（召喚しないときは0）。
        /// 所持中の最高レベルの Value1 体ごとに Value2 体を召喚する。未所持のあいだの撃破は数えない
        /// </summary>
        int NotifyKill();
    }
}
