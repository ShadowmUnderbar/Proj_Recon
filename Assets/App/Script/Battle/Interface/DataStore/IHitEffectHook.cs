using App.Battle.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 命中の前後に走る副作用のフック（最低保証ダメージの設定・命中の通知・ウイルスの付与など）。
    /// <see cref="App.Battle.UseCase.BattleHitUseCase"/> が登録済みの実装を <see cref="Order"/> の小さい順に呼ぶ。
    /// 必要な方だけを実装すればよい（もう片方は何もしない既定実装）。
    /// 「射撃の命中だけ」に反応したい効果は <see cref="HitData.ShotType"/> が null でないことを自分で確認する
    /// （感電の伝播やウイルスのダメージなど、射撃でない命中は ShotType を持たない）
    /// </summary>
    public interface IHitEffectHook
    {
        /// <summary>呼ぶ順番（<see cref="HitHookOrder"/> の定数を使う）。同じ値同士の順番は保証しない</summary>
        int Order { get; }

        /// <summary>
        /// 倍率を掛け終えた後・<c>EnemyDataStore.Damage</c> の前に呼ばれる。
        /// <paramref name="hitData"/> の <see cref="HitData.MinDamage"/> などを書き換えられる
        /// </summary>
        void OnBeforeDamage(HitData hitData)
        {
        }

        /// <summary>
        /// <c>EnemyDataStore.Damage</c> の後に呼ばれる。
        /// </summary>
        /// <param name="hitData">命中情報（倍率適用後のダメージ）</param>
        /// <param name="dealtDamage">実際に減った体力（弱点・耐性・最低保証の後の値。敵が見つからなければ 0）</param>
        /// <param name="isTargetAlive">この命中で撃破されずに残っているか（撃破済み・消えた敵なら false）</param>
        void OnAfterDamage(HitData hitData, float dealtDamage, bool isTargetAlive)
        {
        }
    }
}
