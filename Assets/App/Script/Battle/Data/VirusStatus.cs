namespace App.Battle.Data
{
    /// <summary>
    /// 敵1体に付いているウイルスの現在の状態（読み取り用）
    /// </summary>
    public readonly struct VirusStatus
    {
        public VirusStatus(float power, float maxDuration, float remainingTime)
        {
            Power = power;
            MaxDuration = maxDuration;
            RemainingTime = remainingTime;
        }

        /// <summary>威力（重複のたびに加算される）</summary>
        public float Power { get; }

        /// <summary>最長効果時間（秒）。毎秒ダメージ＝威力÷最長効果時間</summary>
        public float MaxDuration { get; }

        /// <summary>残り効果時間（秒）</summary>
        public float RemainingTime { get; }
    }
}
