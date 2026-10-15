namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// サモンタレット（マージショット時に照準位置へタレットを召喚する）の実行時状態
    /// </summary>
    public interface ISummonTurretDataStore
    {
        /// <summary>次に召喚できるまでの残り秒数。0以下なら召喚できる</summary>
        float RemainingCoolDown { get; }

        /// <summary>所持していてクールダウンが明けているか</summary>
        bool CanSummon();

        /// <summary>
        /// 召喚できたときに呼び、クールダウンを開始する。秒数は所持中の最高レベルの Value1。
        /// 置ける場所が無く出せなかったときは呼ばない（クールダウンを消費しない）
        /// </summary>
        void StartCoolDown();
    }
}
