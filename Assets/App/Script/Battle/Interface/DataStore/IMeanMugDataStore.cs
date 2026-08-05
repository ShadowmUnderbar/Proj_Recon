using System.Collections.Generic;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ガン飛ばしの実行時状態。
    /// 視界中央に捉えている敵が受ける最終ダメージを増加させる（範囲外に出ると即座に効果外）。
    /// </summary>
    public interface IMeanMugDataStore
    {
        /// <summary>
        /// 注視判定に使う半径を返す。ガン飛ばし未所持なら false。
        /// </summary>
        bool TryGetGazeRadius(out float radius);

        /// <summary>
        /// 注視中の敵IDを設定する（毎フレーム置き換える）。
        /// </summary>
        void SetGazedEnemies(IReadOnlyList<int> gazedEnemyIds);

        /// <summary>
        /// 指定した敵が受ける最終ダメージ倍率を返す。注視外・未所持は 1.0f。
        /// </summary>
        float GetDamageMultiplier(int enemyId);
    }
}
