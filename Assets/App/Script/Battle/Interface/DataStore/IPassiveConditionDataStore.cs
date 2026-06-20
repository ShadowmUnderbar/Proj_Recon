namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// パッシブ効果の発動条件が現在成立しているかを判定する単一窓口。
    /// </summary>
    public interface IPassiveConditionDataStore
    {
        /// <summary>
        /// 指定した条件が現在成立しているかを返す。
        /// ConditionType.None は常に true。
        /// </summary>
        /// <param name="conditionType">判定する条件種別</param>
        /// <param name="conditionValue">条件のしきい値／継続秒数（意味は ConditionType ごとに異なる）</param>
        bool IsSatisfied(ConditionType conditionType, float conditionValue);
    }
}
