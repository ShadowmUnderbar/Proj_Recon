using System.Collections.Generic;

namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// デバッグ対戦で出した相手（敵Id）と、出し直しの待ち時間を管理する。
    /// 出す・出し直す流れは <c>DebugArenaUseCase</c> が担う。
    /// </summary>
    public interface IDebugArenaDataStore
    {
        /// <summary>今のランでまだ一度も相手を出していないか（プレイヤーの位置合わせはこのときだけ行う）</summary>
        bool IsFirstSpawnInRun { get; }

        /// <summary>相手を出す（出し直す）時か。ラン開始直後と、全員いなくなって待ち時間が過ぎたとき true</summary>
        bool ShouldSpawn { get; }

        /// <summary>出した相手の敵Id。出現に失敗して空なら、出し直しをやめる（失敗を繰り返さない）</summary>
        void MarkSpawned(IReadOnlyList<int> enemyIds);

        /// <summary>相手が撃破されたことを記録する（出し直すのは、少なくとも1体が撃破されたときだけ）</summary>
        void NotifyEnemyDead(int enemyId);

        /// <summary>撃破・消去された敵を相手から外す（相手以外の敵Idは無視）</summary>
        void NotifyEnemyRemoved(int enemyId);

        /// <summary>
        /// 相手が全員いなくなっていれば出し直しの待ちに入り、待ち時間を進める。
        /// 誰も撃破されずに全員消えた（プレハブの読み込み失敗など）ときは、失敗を繰り返さないよう出し直さない
        /// </summary>
        void Tick(float deltaTime);
    }
}
