namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// アンガーマネジメント（被弾から Value2 秒後に、受けたダメージ × Value1 秒のレイジを得る。レイジ未所持でも発動する）
    /// </summary>
    public interface IAngerManagementDataStore
    {
        /// <summary>
        /// 被弾を通知し、所持中の最高レベルの値で Value2 秒後のレイジ獲得を予約する。
        /// アンガーマネジメント未所持なら何もしない
        /// </summary>
        /// <returns>予約したレイジの秒数（予約しなかったときは0）</returns>
        float NotifyDamaged(float damage);
    }
}
