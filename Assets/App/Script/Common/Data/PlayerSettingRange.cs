namespace App.Common.Data
{
    /// <summary>
    /// プレイヤー設定が取りうる範囲と既定値。
    /// オプションUI（スライダーの上下限）とDataStore（保存時のクランプ）の双方から参照するため、
    /// 値の定義をここ1箇所にまとめている。
    /// </summary>
    public static class PlayerSettingRange
    {
        /// <summary>移動速度[m/s]の下限。これ以下だと部屋の端まで歩くのが苦痛になる</summary>
        public const float MinMoveSpeed = 1f;

        /// <summary>移動速度[m/s]の上限。速すぎるとVRで酔うため抑えている</summary>
        public const float MaxMoveSpeed = 5f;

        /// <summary>移動速度[m/s]の既定値。おおよそ人の早歩き</summary>
        public const float DefaultMoveSpeed = 2.5f;

        /// <summary>スナップターン角度[deg]の下限</summary>
        public const int MinSnapTurnAngle = 15;

        /// <summary>スナップターン角度[deg]の上限</summary>
        public const int MaxSnapTurnAngle = 90;

        /// <summary>スナップターン角度[deg]の刻み。中途半端な角度を選べないようにする</summary>
        public const int SnapTurnAngleStep = 15;

        /// <summary>スナップターン角度[deg]の既定値</summary>
        public const int DefaultSnapTurnAngle = 30;
    }
}
