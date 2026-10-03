namespace App.Battle.Interface.DataStore
{
    /// <summary>
    /// ボスウェーブ（BossWaveConfig で指定したウェーブ）の進行状態。
    /// </summary>
    public interface IBossWaveDataStore
    {
        /// <summary>現在のウェーブがボスウェーブか</summary>
        bool IsBossWave { get; }

        /// <summary>現在のボスウェーブでボスを出現させ済みか</summary>
        bool IsBossSpawned { get; }

        /// <summary>現在のボスウェーブでボスを出現させ、全員いなくなったか（出現・読み込みの失敗で消えた場合も含む）</summary>
        bool IsBossCleared { get; }

        /// <summary>
        /// 全員いなくなり、そのうち少なくとも1体を撃破したか。
        /// 出現・読み込みの失敗で誰も倒さずに消えた場合は false（クリア扱いにしない）
        /// </summary>
        bool IsBossDefeated { get; }

        /// <summary>
        /// 現在のウェーブがボスウェーブで、まだボスを出していなければ出現させる。
        /// 出現させたときだけ true を返す（同じウェーブで二重に出さない）。
        /// </summary>
        bool TrySpawnBoss();

        /// <summary>敵の撃破を記録する（今回出したボスでなければ何もしない）</summary>
        void NotifyEnemyDead(int enemyId);
    }
}
