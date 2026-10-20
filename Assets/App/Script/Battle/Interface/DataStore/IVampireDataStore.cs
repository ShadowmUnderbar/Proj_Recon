namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ヴァンパイア（敵を撃破するたびに体力を Value1 回復する）
    /// </summary>
    public interface IVampireDataStore
    {
        /// <summary>
        /// 敵を1体撃破したときの回復量を返す。所持中の最高レベルの Value1。未所持なら0
        /// </summary>
        float GetHealAmount();
    }
}
