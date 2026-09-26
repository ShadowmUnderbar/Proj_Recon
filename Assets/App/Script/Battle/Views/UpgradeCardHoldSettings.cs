namespace App.Battle.Views
{
    /// <summary>
    /// カードを持ったときの見え方の設定。
    /// ボードの Inspector 値を毎フレーム束ねて渡すことで、Play Mode 中の調整がそのまま効く
    /// </summary>
    public readonly struct UpgradeCardHoldSettings
    {
        /// <summary>持っているときのカードの拡大率</summary>
        public readonly float HoldScale;

        /// <summary>カードが手や定位置へ追いつく速さ</summary>
        public readonly float FollowSpeed;

        /// <summary>手首のひねりの増幅率。1で手首どおり</summary>
        public readonly float WristRollMultiplier;

        public UpgradeCardHoldSettings(float holdScale, float followSpeed, float wristRollMultiplier)
        {
            HoldScale = holdScale;
            FollowSpeed = followSpeed;
            WristRollMultiplier = wristRollMultiplier;
        }
    }
}
