namespace App.Battle.Data
{
    /// <summary>
    /// ボスがプレイヤーに対してつく位置（ワールドの軸基準。上下＝±Z、左右＝±X）。
    /// </summary>
    public enum BossFormationSlot
    {
        /// <summary>配置の指定なし</summary>
        None,

        /// <summary>プレイヤーの +Z 側</summary>
        Up,

        /// <summary>プレイヤーの -Z 側</summary>
        Down,

        /// <summary>プレイヤーの -X 側</summary>
        Left,

        /// <summary>プレイヤーの +X 側</summary>
        Right,

        /// <summary>プレイヤーの -X・+Z 側（斜め）</summary>
        UpLeft,

        /// <summary>プレイヤーの +X・+Z 側（斜め）</summary>
        UpRight,

        /// <summary>プレイヤーの +X・-Z 側（斜め）</summary>
        DownRight,

        /// <summary>プレイヤーの -X・-Z 側（斜め）</summary>
        DownLeft
    }

    public static class BossFormationSlotExtensions
    {
        /// <summary>プレイヤーから見たその位置の方向（水平の単位ベクトル。None は零ベクトル）</summary>
        public static UnityEngine.Vector3 ToDirection(this BossFormationSlot slot)
        {
            return slot switch
            {
                BossFormationSlot.Up => UnityEngine.Vector3.forward,
                BossFormationSlot.Down => UnityEngine.Vector3.back,
                BossFormationSlot.Left => UnityEngine.Vector3.left,
                BossFormationSlot.Right => UnityEngine.Vector3.right,
                BossFormationSlot.UpLeft => new UnityEngine.Vector3(-1f, 0f, 1f).normalized,
                BossFormationSlot.UpRight => new UnityEngine.Vector3(1f, 0f, 1f).normalized,
                BossFormationSlot.DownRight => new UnityEngine.Vector3(1f, 0f, -1f).normalized,
                BossFormationSlot.DownLeft => new UnityEngine.Vector3(-1f, 0f, -1f).normalized,
                _ => UnityEngine.Vector3.zero
            };
        }

        /// <summary>プレイヤーを中心に90度回りこんだ先の位置（None・回りこまないときはそのまま）</summary>
        public static BossFormationSlot Turn90(this BossFormationSlot slot, BossTurnDirection direction)
        {
            var isClockwise = direction == BossTurnDirection.Clockwise;
            if (direction == BossTurnDirection.None)
            {
                return slot;
            }

            return slot switch
            {
                BossFormationSlot.Up => isClockwise ? BossFormationSlot.Right : BossFormationSlot.Left,
                BossFormationSlot.Right => isClockwise ? BossFormationSlot.Down : BossFormationSlot.Up,
                BossFormationSlot.Down => isClockwise ? BossFormationSlot.Left : BossFormationSlot.Right,
                BossFormationSlot.Left => isClockwise ? BossFormationSlot.Up : BossFormationSlot.Down,
                BossFormationSlot.UpLeft => isClockwise ? BossFormationSlot.UpRight : BossFormationSlot.DownLeft,
                BossFormationSlot.UpRight => isClockwise ? BossFormationSlot.DownRight : BossFormationSlot.UpLeft,
                BossFormationSlot.DownRight => isClockwise ? BossFormationSlot.DownLeft : BossFormationSlot.UpRight,
                BossFormationSlot.DownLeft => isClockwise ? BossFormationSlot.UpLeft : BossFormationSlot.DownRight,
                _ => slot
            };
        }

        /// <summary>斜めの位置か</summary>
        public static bool IsDiagonal(this BossFormationSlot slot)
        {
            return slot >= BossFormationSlot.UpLeft;
        }

        /// <summary>縦方向（上下）の位置か</summary>
        public static bool IsVertical(this BossFormationSlot slot)
        {
            return slot == BossFormationSlot.Up || slot == BossFormationSlot.Down;
        }
    }
}
