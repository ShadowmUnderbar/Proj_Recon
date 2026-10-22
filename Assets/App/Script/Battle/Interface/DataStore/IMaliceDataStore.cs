namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// マリス（プレイヤーの周りの敵のウイルスを切らさず、1秒ごとに威力を足していく）
    /// </summary>
    public interface IMaliceDataStore
    {
        /// <summary>
        /// プレイヤーから半径 Value1 m 以内の敵のウイルスの残り効果時間を止め、1秒経つごとに
        /// ウイルスが無ければ威力 Value2・効果時間 Value3 秒で付け、あれば威力だけ Value2 足す。
        /// 所持中の最高レベルのみ採用。未所持なら何もしない。
        /// 残り効果時間を止める指定は次の1回の Advance にだけ効くので、
        /// 毎フレーム、ウイルスの時間を進める（<see cref="IVirusDataStore.Advance"/>）直前に呼ぶこと
        /// </summary>
        void Tick(float deltaTime);
    }
}
