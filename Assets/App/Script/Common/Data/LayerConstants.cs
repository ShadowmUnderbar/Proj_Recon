using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// 物理クエリで使うレイヤーマスク。レイヤー番号は TagManager の名前から引くため、
    /// レイヤーの並びを変えてもここを直す必要はない。
    /// </summary>
    public static class LayerConstants
    {
        /// <summary>地形（Default レイヤー）。回避先・跳ね返し攻撃の遮蔽判定に使う</summary>
        public static readonly int Default = ToMask("Default");

        /// <summary>敵本体とヒットボックス</summary>
        public static readonly int Enemy = ToMask("Enemy");

        // ポイント粒子。地形・敵と違いレイキャストの対象になってはいけない（回避・エイム・視線判定を遮らせない）ため
        // 専用レイヤーに置き、レイヤーマスクを持たない物理クエリ側でこのマスクを明示的に除外する。
        // 粒子を拾いたい処理（弾の通過判定）だけがこのマスクを指定する
        public static readonly int PointParticle = ToMask("PointParticle");

        /// <summary>
        /// レイヤー名からマスクを作る。名前が TagManager に無いと NameToLayer は -1 を返し、
        /// そのままシフトすると 1 &lt;&lt; 31 という無関係なマスクに化けて判定が黙って壊れるため、ここで止める
        /// </summary>
        private static int ToMask(string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                throw new System.InvalidOperationException($"レイヤー「{layerName}」が TagManager に定義されていません");
            }

            return 1 << layer;
        }
    }
}
