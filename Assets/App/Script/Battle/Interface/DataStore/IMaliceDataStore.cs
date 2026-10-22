namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// マリス（1秒ごとに、プレイヤーの周りの敵へウイルスを付与する）
    /// </summary>
    public interface IMaliceDataStore
    {
        /// <summary>
        /// 時間を進め、1秒経つごとにプレイヤーから半径 Value1 m 以内の敵へ威力 Value2・効果時間 Value3 秒のウイルスを付与する。
        /// 所持中の最高レベルのみ採用。未所持なら何もしない。
        /// ウイルスの時間を進める（<see cref="IVirusDataStore.Advance"/>）直前に、同じ deltaTime で呼ぶこと
        /// （付け直しより先にウイルスが切れて威力の積み上がりが途切れないよう、ウイルスと同じ時間の数え方にそろえている）
        /// </summary>
        void Tick(float deltaTime);
    }
}
