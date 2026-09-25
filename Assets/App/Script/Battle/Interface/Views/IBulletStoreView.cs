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
    }
}
