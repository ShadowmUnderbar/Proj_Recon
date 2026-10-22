namespace App.Battle.Interface
{
    /// <summary>
    /// 弾の貸出口。プレハブごとのプールから弾を取り出す（毎回 Instantiate しない）。
    /// 借りた弾は <see cref="IBulletView.Spawn"/> で撃ち、寿命切れ・着弾で自分からプールへ戻る。
    /// プレイヤー・味方・敵・ボスのどの撃ち手もここを通す
    /// </summary>
    public interface IBulletSpawner
    {
        /// <param name="prefab">撃ち手が持つ弾のプレハブ（種類ごとに別のプールになる）</param>
        IBulletView Rent(IBulletView prefab);
    }
}
