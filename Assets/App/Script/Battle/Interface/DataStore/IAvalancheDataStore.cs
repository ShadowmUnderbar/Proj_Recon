using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 雪崩（マージショットの連射を基本弱体化し、命中していれば次発を大幅強化）の実行時状態
    /// </summary>
    public interface IAvalancheDataStore
    {
        /// <summary>
        /// 射撃クールダウンに掛ける倍率を返す。未所持・マージ以外は 1.0f。
        /// 直前のマージショットが命中していれば強化倍率、外していれば弱体倍率を返す。
        /// </summary>
        float GetCoolDownMultiplier(ShotType shotType);

        /// <summary>射撃したことを通知する（マージ射撃時に命中フラグを消費する）</summary>
        void NotifyShot(ShotType shotType);

        /// <summary>マージショットが敵に命中したことを通知する（次発の強化フラグを立てる）</summary>
        void NotifyMergeHit();
    }
}
