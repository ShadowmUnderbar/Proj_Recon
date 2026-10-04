namespace App.Battle.Data
{
    /// <summary>
    /// ボスがプレイヤーのまわりを回りこむ向き（真上から見た向き。ワールドの上下＝±Z、左右＝±X）。
    /// </summary>
    public enum BossTurnDirection
    {
        /// <summary>回りこまない</summary>
        None,

        /// <summary>時計回り（上 → 右 → 下 → 左）</summary>
        Clockwise,

        /// <summary>反時計回り（上 → 左 → 下 → 右）</summary>
        CounterClockwise
    }

    public static class BossTurnDirectionExtensions
    {
        /// <summary>Y軸まわりの回転角の符号（時計回りが正。None は0）</summary>
        public static float ToSign(this BossTurnDirection direction)
        {
            return direction switch
            {
                BossTurnDirection.Clockwise => 1f,
                BossTurnDirection.CounterClockwise => -1f,
                _ => 0f
            };
        }
    }
}
