using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// デバッグ対戦（通常のウェーブ進行をせず、指定した敵・ボスグループとだけ戦う）の設定。
    /// 値は LifetimeScope がバトルの組み立て時に DebugConfig の予約から作って注入する（利用側は DebugConfig を直接参照しない）。
    /// 予約が無いときは <see cref="Disabled"/> が注入され、通常のランになる。
    /// </summary>
    public class DebugArenaSettings
    {
        public static DebugArenaSettings Disabled { get; } = new(false, null, string.Empty, 0, 1, false, 0f, false);

        /// <summary>デバッグ対戦として組み立てたか。false なら他の値は使わない</summary>
        public bool IsEnabled { get; }

        /// <summary>出すボスグループ。null なら <see cref="EnemyCode"/> の敵を出す</summary>
        public BossGroupConfig BossGroup { get; }

        /// <summary>出す敵の EnemyMasterDataId（<see cref="BossGroup"/> があるときは使わない）</summary>
        public string EnemyCode { get; }

        /// <summary>同時に出す敵の数（<see cref="BossGroup"/> があるときは使わない）</summary>
        public int EnemyCount { get; }

        /// <summary>対戦するウェーブ番号（1始まり）</summary>
        public int Wave { get; }

        /// <summary>出した相手が全員いなくなったら出し直す</summary>
        public bool AutoRespawn { get; }

        /// <summary>全員いなくなってから出し直すまでの秒数</summary>
        public float RespawnDelaySeconds { get; }

        /// <summary>プレイヤーの HP を減らさない（被弾の通知は流す）</summary>
        public bool Invincible { get; }

        public DebugArenaSettings(
            bool isEnabled,
            BossGroupConfig bossGroup,
            string enemyCode,
            int enemyCount,
            int wave,
            bool autoRespawn,
            float respawnDelaySeconds,
            bool invincible
        )
        {
            IsEnabled = isEnabled;
            BossGroup = bossGroup;
            EnemyCode = enemyCode ?? string.Empty;
            EnemyCount = Mathf.Max(1, enemyCount);
            Wave = Mathf.Max(1, wave);
            AutoRespawn = autoRespawn;
            RespawnDelaySeconds = Mathf.Max(0f, respawnDelaySeconds);
            Invincible = invincible;
        }

        /// <summary>ラン開始時（リスタートを含む）のウェーブ番号。通常のランは 1、デバッグ対戦は指定したウェーブ</summary>
        public int StartWave => IsEnabled ? Wave : 1;

        /// <summary>デバッグ対戦中なら HP を減らさない</summary>
        public bool IsPlayerInvincible => IsEnabled && Invincible;
    }
}
