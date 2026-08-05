using System.Collections.Generic;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// スネークアイズの実行時状態。
    /// 視界中央に捉えている敵の速度を落とし、範囲外に出てから一定時間後に元へ戻す。
    /// </summary>
    public interface ISnakeEyesDataStore
    {
        /// <summary>
        /// 注視判定に使う半径を返す。スネークアイズ未所持なら false。
        /// </summary>
        bool TryGetGazeRadius(out float radius);

        /// <summary>
        /// 注視中の敵IDを更新し、速度倍率が変化した敵だけを返す。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<(int enemyId, float speedMultiplier)> UpdateGazedEnemies(IReadOnlyList<int> gazedEnemyIds);

        /// <summary>
        /// 敵の消滅時に状態を破棄する。
        /// </summary>
        void RemoveEnemy(int enemyId);
    }
}
