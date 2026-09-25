namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ウェーブ進行による敵強化倍率の算出。スポーン時のHP・攻撃力の決定に使う。
    /// </summary>
    public interface IEnemyWaveScalingCalculatorDataStore
    {
        /// <summary>
        /// 基礎HP・基礎攻撃力に現在のウェーブの倍率を掛け、小数切り上げした値を返す
        /// </summary>
        void GetScaledStatus(float baseHp, float baseDamage, out float scaledHp, out float scaledDamage);
    }
}
