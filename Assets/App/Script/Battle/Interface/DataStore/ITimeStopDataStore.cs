using R3;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ボスによる時止めの実行時状態。
    /// 時止め中はプレイヤーの移動・射撃・回避と弾が止まり、プレイヤーは被弾しない。ボスは動ける（フリーズとの違い）。
    /// 停止の適用は <c>FreezeUseCase</c>（弾・レイ）と、プレイヤー側の各UseCaseの入口での判定が担う。
    /// </summary>
    public interface ITimeStopDataStore
    {
        /// <summary>時止め中かどうか</summary>
        ReadOnlyReactiveProperty<bool> IsTimeStopped { get; }

        /// <summary>時止めを始める・解く</summary>
        void SetTimeStop(bool isTimeStopped);
    }
}
