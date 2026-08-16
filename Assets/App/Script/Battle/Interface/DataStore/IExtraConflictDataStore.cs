using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// エクスコンフリクト（ワルツ・マージ・フォーカスを封印する代わりに、ダメージと連射速度を強化する）の実行時状態。
    /// レベルは累積せず、所持中の最高レベルのみを採用する。
    /// </summary>
    public interface IExtraConflictDataStore
    {
        /// <summary>指定した射撃フォームが封印されているか（封印中は選択も発射もできない）</summary>
        bool IsShotTypeLocked(ShotType shotType);

        /// <summary>フォーカスが封印されているか</summary>
        bool IsFocusLocked { get; }

        /// <summary>弾ダメージに掛ける倍率（未所持なら 1.0）</summary>
        float GetDamageMultiplier();

        /// <summary>射撃クールダウンに掛ける倍率（未所持なら 1.0）</summary>
        float GetCoolDownMultiplier();
    }
}
