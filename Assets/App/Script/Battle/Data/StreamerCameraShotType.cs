namespace App.Battle.Data
{
    /// <summary>配信用カメラの演出ショットの構図種別</summary>
    public enum StreamerCameraShotType
    {
        /// <summary>被写体を正面から狙う固定画角</summary>
        Static,

        /// <summary>被写体を中心にゆっくり回り込む</summary>
        Orbit,

        /// <summary>プレイヤーの肩越しに被写体を収める</summary>
        OverShoulder,
    }
}
