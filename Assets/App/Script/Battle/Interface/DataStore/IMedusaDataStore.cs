using System.Collections.Generic;
using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// メデューサの実行時状態。
    /// 視界中央に捉えたメジャークラス以上の敵を一定時間スタンさせる（敵ごとに1度だけ）。
    /// </summary>
    public interface IMedusaDataStore
    {
        /// <summary>
        /// 注視判定に使う半径を返す。メデューサ未所持なら false。
        /// </summary>
        bool TryGetGazeRadius(out float radius);

        /// <summary>
        /// スタン対象となる敵ランクか。
        /// </summary>
        bool IsStunTargetRank(EnemyRankType rankType);

        /// <summary>
        /// スタン対象の敵IDを更新し、スタン状態が変化した敵だけを返す（true=開始 / false=終了）。
        /// 戻り値は呼び出しごとに再利用する内部リスト（次の呼び出しで上書きされる）。
        /// </summary>
        IReadOnlyList<(int enemyId, bool isStun)> UpdateStunTargets(IReadOnlyList<int> stunTargetEnemyIds);

        /// <summary>
        /// 敵の消滅時に状態を破棄する。
        /// </summary>
        void RemoveEnemy(int enemyId);
    }
}
