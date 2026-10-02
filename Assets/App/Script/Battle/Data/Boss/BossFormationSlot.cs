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
