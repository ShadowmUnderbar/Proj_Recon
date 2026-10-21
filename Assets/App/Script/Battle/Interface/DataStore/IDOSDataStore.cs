namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// DOS（射撃の命中で、確率でウイルスを付与する）
    /// </summary>
    public interface IDOSDataStore
    {
        /// <summary>
        /// 射撃が命中した敵に、所持中の最高レベルの確率（Value1）でウイルス（威力 Value2・効果時間 Value3 秒）を付与する。
        /// 未所持・確率に外れたら何もしない
        /// </summary>
        void TryInfect(int enemyId);
    }
}
