namespace App.Battle.Interface
{
    public interface IBulletStoreView
    {
        void AllRemove();

        /// <summary>
        /// 現在飛んでいる弾をその場で止める／再開する（フリーズ用）。
        /// 呼び出した時点で存在する弾に適用するため、フリーズ中に新しく生まれた弾は止まらない。
        /// </summary>
        void SetPause(bool isPause);

        /// <summary>
        /// オーバークロックの開始・終了を現在の弾に反映する。
        /// 敵弾はその場で止め、自弾は飛ばし続ける。どちらも寿命とトレイルを止めて軌跡を残す。
        /// 発動中に撃たれた自弾は、弾自身が生成時に <see cref="ITracerFreezeState.IsOverclock"/> を見て合わせる。
        /// </summary>
        void SetOverclock(bool isActive);
    }
}
