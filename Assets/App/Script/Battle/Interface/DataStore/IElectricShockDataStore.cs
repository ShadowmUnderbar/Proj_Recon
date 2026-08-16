using App.Battle.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 感電（ワルツショットの命中時、命中した敵の周囲へダメージを伝播させる）の実行時状態。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public interface IElectricShockDataStore
    {
        /// <summary>
        /// ワルツショットの命中に対して、周囲へ伝播させる内容を返す。
        /// 未所持・ワルツ以外の命中・対象なしの場合は false。
        /// 伝播先リストは内部で使い回すため、呼び出し側は即座に消費すること。
        /// </summary>
        bool TryGetChain(HitData hitData, out ElectricShockChain chain);
    }
}
