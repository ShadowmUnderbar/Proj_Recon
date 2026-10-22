using App.Battle.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 射撃・弾の命中ダメージに掛ける倍率を提供するフック。
    /// <see cref="App.Battle.UseCase.BattleHitUseCase"/> が登録済みの実装を <see cref="Order"/> の小さい順に全て掛ける
    /// （弱点・耐性の倍率と最低保証は、その後の <c>EnemyDataStore.Damage</c> で掛かる）。
    /// アップグレードの DataStore がこれを実装して <c>AsImplementedInterfaces()</c> で登録するだけで命中処理につながる。
    /// 加算・減算を混ぜたい効果は、この仕組みに乗せる前に計算式を提示して判断を仰ぐこと
    /// </summary>
    public interface IHitDamageMultiplierHook
    {
        /// <summary>掛ける順番（<see cref="HitHookOrder"/> の定数を使う）。同じ値同士の順番は保証しない</summary>
        int Order { get; }

        /// <summary>この命中に掛ける倍率。効果なし・対象外は 1.0f</summary>
        float GetDamageMultiplier(HitData hitData);
    }
}
