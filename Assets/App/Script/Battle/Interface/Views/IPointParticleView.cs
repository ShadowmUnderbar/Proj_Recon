namespace App.Battle.Interface
{
    /// <summary>
    /// 漂っているポイント粒子1個。プレイヤーの接触に加えて、弾が通過したときにも回収される。
    /// </summary>
    public interface IPointParticleView
    {
        /// <summary>回収済みか（回収済みの粒子は二重に加算しない）</summary>
        bool IsCollected { get; }

        /// <summary>回収する。実際のポイント加算と破棄は PointParticleStoreView が次の更新でまとめて行う</summary>
        void Collect();
    }
}
