namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ラフレシア（ウイルスを持ったまま撃破された敵から、周りの敵へウイルスを広げる）
    /// </summary>
    public interface IRafflesiaDataStore
    {
        /// <summary>
        /// 撃破された敵がウイルスを持っていれば、その敵から半径 Value1 m 以内の他の敵に
        /// 威力 × Value2・効果時間「残り効果時間 + Value3 秒」のウイルスを付与する。
        /// 所持中の最高レベルのみ採用。未所持・ウイルスが無ければ何もしない。
        /// 撃破された敵の敵データ・ウイルスが取り除かれる前（BattleHitUseCase.OnEnemyDead の死亡演出の await より前）に呼ぶこと
        /// </summary>
        void Spread(int deadEnemyId);
    }
}
