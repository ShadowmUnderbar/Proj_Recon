namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 食いしばりの効果判定。
    /// 致死ダメージを受けた際、そのダメージを無効化して体力1で耐える（1ウェーブに Value1 回まで）。
    /// レイジ発動中なら、さらにレイジの残り秒数 × Value2 だけ体力を回復する。
    /// </summary>
    public interface IGritDataStore
    {
        /// <summary>
        /// 致死ダメージを耐えるのを試みる。成功した場合はこのウェーブの発動回数を1消費し、耐えた後の体力を返す。
        /// 食いしばり未所持、またはこのウェーブの発動回数を使い切っていれば false。
        /// </summary>
        bool TryActivate(out float health);
    }
}
