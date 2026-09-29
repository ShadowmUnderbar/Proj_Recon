namespace App.Common.Views
{
    /// <summary>
    /// 注視判定の設定。
    /// 使う側の View の Inspector 値を毎フレーム束ねて渡すことで、Play Mode 中の調整がそのまま効く
    /// </summary>
    public readonly struct GazeDetectorSettings
    {
        /// <summary>見ていない状態から「見た」とみなす余白角度[度]。対象の縁からこの角度まで外れていても当たりとする</summary>
        public readonly float EnterMarginAngle;

        /// <summary>
        /// 見ている状態から「外した」とみなす余白角度[度]。EnterMarginAngle より大きくして、境目でのちらつきを防ぐ。
        /// EnterMarginAngle より小さい値は EnterMarginAngle として扱う
        /// </summary>
        public readonly float ExitMarginAngle;

        /// <summary>視線が当たり続けてから「見た」に切り替わるまでの時間[s]。0 なら即座に切り替える</summary>
        public readonly float EnterDelay;

        /// <summary>視線が外れ続けてから「外した」に切り替わるまでの時間[s]。0 なら即座に切り替える</summary>
        public readonly float ExitDelay;

        public GazeDetectorSettings(float enterMarginAngle, float exitMarginAngle, float enterDelay, float exitDelay)
        {
            EnterMarginAngle = enterMarginAngle;
            ExitMarginAngle = exitMarginAngle;
            EnterDelay = enterDelay;
            ExitDelay = exitDelay;
        }
    }
}
