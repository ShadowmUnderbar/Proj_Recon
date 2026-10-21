namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// アンチマテリアル（射撃の命中の最低保証ダメージを Value1 に引き上げる）
    /// </summary>
    public interface IAntiMaterialDataStore
    {
        /// <summary>
        /// 射撃の命中の最低保証ダメージ。所持中の最高レベルの Value1。
        /// 未所持、または Value1 が既定値より小さいときは既定値（<see cref="App.Battle.Data.HitData.DefaultMinDamage"/>）
        /// </summary>
        float GetMinDamage();
    }
}
