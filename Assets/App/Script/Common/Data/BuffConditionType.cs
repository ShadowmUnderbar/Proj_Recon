namespace App.Common.Data
{
    /// <summary>
    /// バフの発動条件の種類
    /// </summary>
    public enum BuffConditionType
    {
        None = 0,

        /// <summary>自攻撃が敵に規定回数ヒットしたら一定時間発動（ConditionValue=必要ヒット数、Duration=効果時間）</summary>
        HitCount = 1,

        /// <summary>自HP割合が閾値以下の間発動し続ける（ConditionValue=HP割合閾値 0〜1）</summary>
        HealthRatioBelow = 2,
    }
}
