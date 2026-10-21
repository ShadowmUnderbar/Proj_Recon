namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// コーラサワーの効果判定。
    /// 致死ダメージを受けた際、確率でそのダメージを無効化する。
    /// 確率は Value1 から始まり、発動するたびに Value2 ずつ下がる。ウェーブが変わると元に戻る。
    /// </summary>
    public interface IColasourDataStore
    {
        /// <summary>
        /// 今のウェーブで致死ダメージを無効化する確率（0〜1）。未所持なら0
        /// </summary>
        float GetChance();

        /// <summary>
        /// 致死ダメージの無効化を試みる。確率に当たれば true を返し、このウェーブの確率を Value2 下げる。
        /// 未所持・確率に外れたら false
        /// </summary>
        bool TryActivate();
    }
}
