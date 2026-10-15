using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 召喚した味方の実行時データ（出現・体力・撃破）。
    /// 味方はラン限りで、リスタート時に全員消える
    /// </summary>
    public interface IAllyDataStore
    {
        /// <summary>味方を出したときに流れる。View への生成はこれを受けた UseCase が行う</summary>
        Observable<AllyData> OnAllySpawned { get; }

        /// <summary>味方が消えたとき（撃破・一括消去・出現失敗）に Id が流れる</summary>
        Observable<int> OnAllyRemoved { get; }

        /// <summary>
        /// 味方を出す。種類のパラメータが <see cref="AllyConfig"/> に無ければ出さずに false を返す
        /// </summary>
        /// <param name="forward">出現時に向ける方向（水平成分だけ使う）。ゼロなら向きを指定しない</param>
        bool TrySpawn(AllyType allyType, Vector3 position, Vector3 forward, out AllyData allyData);

        bool TryGetAllyData(int allyId, out AllyData allyData);

        /// <summary>味方へダメージを与え、体力が0以下になったら消す</summary>
        void Damage(int allyId, float damage);

        void Remove(int allyId);

        void RemoveAll();
    }
}
