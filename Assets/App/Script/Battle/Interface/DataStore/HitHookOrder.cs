namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 命中フック（<see cref="IHitDamageMultiplierHook"/> / <see cref="IHitEffectHook"/>）の適用順。
    /// ダメージの計算順はここだけを見れば分かるようにする。新しい効果を足すときは
    /// 既存のどれの前後に入るかを決めてここに定数を追加する（間隔は後から差し込めるよう 100 刻み）
    /// </summary>
    public static class HitHookOrder
    {
        // ---- 倍率（IHitDamageMultiplierHook）: 全て乗算。この順に掛けた後、弱点・耐性 → 最低保証 ----

        /// <summary>ブルズアイ: 貫通した数に応じた倍率</summary>
        public const int Bullseye = 100;

        /// <summary>ガン飛ばし: 注視中の敵が受ける倍率</summary>
        public const int MeanMug = 200;

        /// <summary>クリティカル（ラッキーチャンス・キリングコール・ターンテーブル）</summary>
        public const int CriticalHit = 300;

        // ---- 副作用（IHitEffectHook）----

        /// <summary>アンチマテリアル: 射撃の最低保証ダメージを引き上げる（Damage の前）</summary>
        public const int AntiMaterial = 100;

        /// <summary>雪崩: マージ命中の通知（Damage の前）</summary>
        public const int Avalanche = 200;

        /// <summary>DOS: 射撃の命中で確率でウイルスを付与（Damage の後）</summary>
        public const int DOS = 300;

        /// <summary>毒舌: 射撃で与えたダメージに応じたウイルスを付与（Damage の後）</summary>
        public const int Invective = 400;
    }
}
