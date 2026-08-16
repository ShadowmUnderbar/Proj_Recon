using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// コンフリクト系アップグレード（特定の射撃手段を封印する代わりに性能を強化する）の実行時状態。
    /// エクスコンフリクト・フォーカスコンフリクトなど複数を同時に所持でき、
    /// 封印は論理和、強化倍率は積で合成する。
    /// </summary>
    public interface IShotConflictDataStore
    {
        /// <summary>指定した射撃フォームが封印されているか（封印中は選択も発射もできない）</summary>
        bool IsShotTypeLocked(ShotType shotType);

        /// <summary>フォーカスが封印されているか</summary>
        bool IsFocusLocked { get; }

        /// <summary>二丁拳銃が封印されているか（利き手のみの射撃に制限されるか）</summary>
        bool IsAkimboLocked { get; }

        /// <summary>弾ダメージに掛ける倍率（未所持なら 1.0）</summary>
        float GetDamageMultiplier();

        /// <summary>射撃クールダウンに掛ける倍率（未所持なら 1.0）</summary>
        float GetCoolDownMultiplier();
    }
}
