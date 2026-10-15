using App.Battle.Data;
using App.Common.Data;
using R3;

namespace App.Battle.Interface
{
    /// <summary>
    /// 召喚した味方の生成・破棄と、味方からの入力（被弾・攻撃の要求）の窓口
    /// </summary>
    public interface IAllyStoreView
    {
        /// <summary>味方が敵の攻撃を受けた（Id, ダメージ）</summary>
        Observable<(int allyId, float damage)> OnAllyDamaged { get; }

        /// <summary>味方が攻撃しようとしている。<see cref="Shot"/> で弾データを渡すと撃つ</summary>
        Observable<int> OnShotRequested { get; }

        /// <summary>置ける場所が無い・プレハブが無いなどで出せなかった</summary>
        Observable<int> OnAllySpawnFailed { get; }

        void Spawn(AllyData allyData);

        void UnSpawn(int allyId);

        /// <summary>要求した味方に弾を撃たせる。狙う敵は味方自身が決めている</summary>
        void Shot(int allyId, BulletData bulletData);

        /// <summary>移動・攻撃を止める／再開する</summary>
        void SetPause(bool isPause);
    }
}
