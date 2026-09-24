using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// 一時停止（フリーズ）の実行時状態。
    /// フリーズ中は敵・プレイヤー・弾がその場で止まる（ヒットストップ等の演出に使う）。
    /// 停止対象の適用は <c>FreezeUseCase</c> と、各UseCaseの入口での判定が担う。
    /// </summary>
    public interface IFreezeDataStore
    {
        /// <summary>フリーズ中かどうか</summary>
        ReadOnlyReactiveProperty<bool> IsFreezing { get; }

        /// <summary>
        /// 指定秒数だけフリーズさせる。
        /// すでにフリーズ中の場合は残り時間の長い方を採用する（短い指定で上書きしない）。
        /// </summary>
        /// <param name="duration">フリーズさせる秒数（0以下なら何もしない）</param>
        void Freeze(float duration);

        /// <summary>フリーズを即座に解除する</summary>
        void Cancel();
    }
}
