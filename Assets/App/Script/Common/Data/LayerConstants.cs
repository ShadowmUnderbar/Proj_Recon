namespace App.Common.Data
{
    public static class LayerConstants
    {
        public static int Default = 1 << 0;
        public static int Hitbox = 1 << 6;

        // ポイント粒子。地形・敵と違いレイキャストの対象になってはいけない（回避・エイム・視線判定を遮らせない）ため
        // 組み込みの Ignore Raycast レイヤーに置く。粒子を明示的に拾いたい処理だけがこのマスクを指定する
        public static int PointParticle = 1 << 2;
    }
}