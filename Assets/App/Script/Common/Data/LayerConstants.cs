namespace App.Common.Data
{
    public static class LayerConstants
    {
        public static int Default = 1 << 0;
        public static int Hitbox = 1 << 6;

        // ポイント粒子。地形・敵と違いレイキャストの対象になってはいけない（回避・エイム・視線判定を遮らせない）ため
        // 専用レイヤーに置き、レイヤーマスクを持たない物理クエリ側でこのマスクを明示的に除外する。
        // 粒子を拾いたい処理（弾の通過判定）だけがこのマスクを指定する
        public static int PointParticle = 1 << 8;
    }
}