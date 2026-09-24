namespace App.Battle.Interface
{
    /// <summary>
    /// 漂っているポイント粒子1個。プレイヤーの接触に加えて、弾が通過したときにも回収される。
    /// </summary>
    public interface IPointParticleView
    {
        /// <summary>回収済みか（回収済みの粒子は二重に加算しない）</summary>
        bool IsCollected { get; }

        /// <summary>弾が当たってプレイヤーへ吸い込まれている最中か</summary>
        bool IsPulling { get; }

        /// <summary>
        /// 弾が当たったときの吸い込みを開始する。プレイヤーへ吸い込まれ切ってから回収される
        /// （実際のポイント加算と破棄は PointParticleStoreView が行う）
        /// </summary>
        void StartPull();
    }
}
