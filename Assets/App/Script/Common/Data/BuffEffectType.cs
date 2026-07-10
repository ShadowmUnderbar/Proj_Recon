namespace App.Common.Data
{
    /// <summary>
    /// バフの効果の種類
    /// </summary>
    public enum BuffEffectType
    {
        None = 0,

        /// <summary>攻撃力倍率（弾ダメージ。爆発ダメージも弾ダメージを共用するため両方に効く）</summary>
        AttackPower = 1,
    }
}
