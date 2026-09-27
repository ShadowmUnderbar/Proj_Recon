namespace App.Battle.Data
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

        /// <summary>一定時間経過後。非利き手の脇へ追従し、常にプレイヤーの方を向く</summary>
        HandFollow,
    }
}
