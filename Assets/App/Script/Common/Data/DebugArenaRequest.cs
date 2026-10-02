using System;

namespace App.Common.Data
{
    /// <summary>
    /// デバッグ対戦（任意の敵・ボスグループと戦う）の予約内容。
    /// エディタのウィンドウ（App/デバッグ: 敵と対戦）が JSON にして EditorPrefs に置き、
    /// 次に組み立てるバトルのスコープが1回だけ読み取る（<see cref="DebugConfig.ConsumeDebugArenaRequestJson"/>）。
    /// バトル側はアセットパスを解決した <c>DebugArenaSettings</c> として受け取る。
    /// </summary>
    [Serializable]
    public class DebugArenaRequest
    {
        /// <summary>出すボスグループ（BossGroupConfig）のアセットパス。空なら <see cref="EnemyCode"/> の敵を出す</summary>
        public string BossGroupAssetPath = string.Empty;

        /// <summary>出す敵の EnemyMasterDataId（ボスグループを指定したときは使わない）</summary>
        public string EnemyCode = string.Empty;

        /// <summary>同時に出す敵の数（ボスグループを指定したときは使わない）</summary>
        public int EnemyCount = 1;

        /// <summary>出した相手が全員いなくなったら、同じ相手を出し直すか</summary>
        public bool AutoRespawn = true;

        /// <summary>全員いなくなってから出し直すまでの秒数</summary>
        public float RespawnDelaySeconds = 3f;

        /// <summary>プレイヤーの HP を減らさない（被弾の通知は流す）</summary>
        public bool Invincible = true;

        public bool IsBossGroup => !string.IsNullOrEmpty(BossGroupAssetPath);
    }
}
