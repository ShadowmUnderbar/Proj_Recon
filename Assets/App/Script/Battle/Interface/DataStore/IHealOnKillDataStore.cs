using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    public interface IHealOnKillDataStore
    {
        /// <summary>
        /// 撃破した敵のランクに応じた回復量を返す。所持中の最高レベルの HealOnKill を採用（非スタック）。
        /// マイナー撃破時は Value1、メジャー撃破時は Value2。対象外ランク・未所持は 0。
        /// </summary>
        float GetHealAmount(EnemyRankType rankType);
    }
}
