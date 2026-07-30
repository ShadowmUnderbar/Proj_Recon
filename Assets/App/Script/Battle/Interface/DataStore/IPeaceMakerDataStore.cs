using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ピースメイカー（ノーマルショット連射強化＋連続射撃でのデメリット）の実行時状態
    /// </summary>
    public interface IPeaceMakerDataStore
    {
        /// <summary>
        /// 射撃クールダウンに掛ける倍率を返す。未所持・対象外フォームは 1.0f。
        /// 連続射撃数が規定値に達している間は強化ではなくペナルティ倍率を返す。
        /// </summary>
        float GetCoolDownMultiplier(ShotType shotType);

        /// <summary>射撃したことを通知する（連続ノーマルショット数の更新・ペナルティの消費）</summary>
        void NotifyShot(ShotType shotType);
    }
}
