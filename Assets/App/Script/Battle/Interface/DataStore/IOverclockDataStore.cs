using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// オーバークロックの実行時状態。
    /// ストックした秒数がしきい値を超えると自動で発動し、ストック秒数ぶん敵・敵弾を止める。
    /// 発動中の被弾はダメージだけを溜め、終了時に1回のダメージとして処理する。
    /// 停止対象への適用は <c>FreezeUseCase</c>、獲得と終了時の処理は <c>OverclockUseCase</c> が担う。
    /// </summary>
    public interface IOverclockDataStore
    {
        /// <summary>発動待ちのストック秒数</summary>
        ReadOnlyReactiveProperty<float> StockSeconds { get; }

        /// <summary>発動中かどうか</summary>
        ReadOnlyReactiveProperty<bool> IsActive { get; }

        /// <summary>発動中の残り秒数（非発動中は0）</summary>
        float RemainingTime { get; }

        /// <summary>
        /// ストック秒数を加算する。発動中は加算しない。
        /// 加算後にしきい値を超えたら、ストックをすべて使って発動する。
        /// </summary>
        void AddStock(float seconds);

        /// <summary>発動中に受けたダメージを溜める</summary>
        void AddStockedDamage(float damage);

        /// <summary>溜めたダメージを取り出して0に戻す</summary>
        float ConsumeStockedDamage();

        /// <summary>残り時間に関係なく発動を終える（ウェーブ終了時など）</summary>
        void ForceEnd();
    }
}
