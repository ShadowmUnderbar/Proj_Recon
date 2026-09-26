namespace App.Common.Data
{
    /// <summary>
    /// ベクトル計算で共有するしきい値。
    /// 「水平成分が消えた」「向きが定まらない」の判定を各Viewで同じ基準にそろえるために一箇所に置く
    /// </summary>
    public static class VectorConstants
    {
        /// <summary>方向ベクトルの二乗長がこれ以下なら、実質ゼロ（向きが定まらない）とみなす</summary>
        public const float DirectionEpsilon = 1e-6f;
    }
}
