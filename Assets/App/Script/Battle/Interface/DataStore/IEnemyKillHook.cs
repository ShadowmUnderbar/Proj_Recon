using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 敵を撃破したとき（<c>EnemyDataStore.OnEnemyDead</c>）に走るフック。
    /// <see cref="App.Battle.UseCase.BattleHitUseCase"/> が登録済みの実装を全て呼ぶ。
    /// 呼ばれるのは死亡演出の前で、撃破された敵のデータ・ウイルスはまだ残っている。
    /// 必要な方だけを実装すればよい（もう片方は何もしない既定実装）。
    /// 撃破は <c>EnemyDataStore.Damage</c> で体力0になったもの（味方の撃破も含む）
    /// </summary>
    public interface IEnemyKillHook
    {
        /// <summary>
        /// 撃破1体あたりの回復量。全フックの合計を1回で <c>IPlayerStateDataStore.Heal</c> する（体力の書き換えを1回にするため）。
        /// 効果なし・対象外は 0
        /// </summary>
        /// <param name="rankType">撃破した敵のランク（マスターデータが引けなければ <see cref="EnemyRankType.None"/>）</param>
        float GetHealAmount(EnemyRankType rankType) => 0f;

        /// <summary>撃破時の副作用（周囲への伝播など）。回復の後に呼ばれる</summary>
        void OnEnemyKilled(int enemyId)
        {
        }
    }
}
