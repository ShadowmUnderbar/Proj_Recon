using App.Battle.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// クリティカルヒットの判定。
    /// ラッキーチャンス・キリングコール・ターンテーブルで得たクリティカル確率を合算して抽選する。
    /// </summary>
    public interface ICriticalHitDataStore
    {
        /// <summary>
        /// この命中に適用するクリティカルのダメージ倍率を返す。
        /// 非クリティカル時は 1.0f。
        /// </summary>
        float GetDamageMultiplier(HitData hitData);
    }
}
