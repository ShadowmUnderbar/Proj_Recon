namespace App.Common.Data
{
    /// <summary>
    /// VRでの移動方式。メインメニューの部屋を歩き回るときに使い、オプションで切り替える。
    /// </summary>
    public enum LocomotionType
    {
        /// <summary>左スティックで滑らかに歩く。酔いやすいが操作は直感的</summary>
        Smooth = 0,

        /// <summary>レイで床を指して瞬間移動する。酔いにくい</summary>
        Teleport = 1,
    }
}
