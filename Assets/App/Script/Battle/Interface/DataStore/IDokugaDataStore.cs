namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 毒牙の効果判定。
    /// 回避中に体が触れた敵へ回避時跳ね返し攻撃のダメージが入ったとき、その敵にウイルスを付与する。
    /// </summary>
    public interface IDokugaDataStore
    {
        /// <summary>
        /// 回避で接触ダメージを与えた敵にウイルスを付与する。毒牙未所持なら何もしない
        /// </summary>
        void Infect(int enemyId);
    }
}
