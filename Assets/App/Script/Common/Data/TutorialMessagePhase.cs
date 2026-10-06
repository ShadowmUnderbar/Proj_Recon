namespace App.Common.Data
{
    /// <summary>
    /// チュートリアルメッセージの配置フェーズ
    /// </summary>
    public enum TutorialMessagePhase
    {
        /// <summary>非表示</summary>
        Hidden,

        /// <summary>表示直後。視点の正面へ追従する</summary>
        HeadFollow,

        /// <summary>一定時間経過後。非利き手の手のひら側へ追従し、向きも手に固定する</summary>
        HandFollow,
    }
}
