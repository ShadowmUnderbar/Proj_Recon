using System.Collections.Generic;
using App.Battle.Data;
using App.Common.Data.MasterData;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 撃破した敵のポイントドロップ量算出と、ドロップ量の粒子単位への分割。
    /// </summary>
    public interface IPointDropCalculatorDataStore
    {
        /// <summary>
        /// 撃破した敵のドロップ量。個別設定（1以上）があればそれを、無ければランク別の既定値を使う
        /// </summary>
        int GetDropPoint(EnemyMasterData enemyMasterData);

        /// <summary>
        /// ドロップ量を粒子の単位へ分割して results に詰める（呼び出し側のリストを使い回すためアロケーションしない）
        /// </summary>
        void Split(int amount, List<PointUnitData> results);
    }
}
