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

        /// <summary>現在のボスウェーブでボスを出現させ、全員倒し終えたか</summary>
        bool IsBossCleared { get; }

        /// <summary>
        /// 現在のウェーブがボスウェーブで、まだボスを出していなければ出現させる。
        /// 出現させたときだけ true を返す（同じウェーブで二重に出さない）。
        /// </summary>
        bool TrySpawnBoss();
    }
}
