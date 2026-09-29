namespace App.Battle.Data
{
    /// <summary>
    /// 注視判定（スネークアイズ・メデューサ・ガン飛ばし）のデバッグ設定。
    /// 値は LifetimeScope が再生開始時に DebugConfig から読んで注入する（利用側は DebugConfig を直接参照しない）
    /// </summary>
    public class EnemyGazeDebugSettings
    {
        /// <summary>視線が敵の判定球を通った瞬間に、その敵へ被弾リアクションを出す（判定球の大きさの確認用）</summary>
        public bool PlayHitFeedbackOnGazeTouch { get; }

        public EnemyGazeDebugSettings(bool playHitFeedbackOnGazeTouch)
        {
            PlayHitFeedbackOnGazeTouch = playHitFeedbackOnGazeTouch;
        }
    }
}
