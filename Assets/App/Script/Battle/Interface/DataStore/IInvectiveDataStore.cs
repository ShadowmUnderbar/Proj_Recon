namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 毒舌（射撃で与えたダメージに応じたウイルスを付与する）
    /// </summary>
    public interface IInvectiveDataStore
    {
        /// <summary>
        /// 射撃で与えたダメージ × Value1 を威力・効果時間（秒）とするウイルスを付与する。
        /// レイジ発動中は威力だけ Value2 倍にする。所持中の最高レベルのみ採用。未所持・ダメージ0以下なら何もしない
        /// </summary>
        void Infect(int enemyId, float dealtDamage);
    }
}
