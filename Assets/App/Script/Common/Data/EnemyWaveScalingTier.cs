using System;
using UnityEngine;

namespace App.Common.Data
{
    /// <summary>
    /// ウェーブ進行による敵強化の増加率を、ウェーブ帯ごとに切り替えるための1段階ぶんの設定。
    /// 「5ウェーブ目からは増加率を上げる」といった調整をマスターデータ側で行えるようにする。
    /// </summary>
    [Serializable]
    public class EnemyWaveScalingTier
    {
        [SerializeField, Min(1), Tooltip("この段階の増加率が適用され始めるウェーブ番号（1以上）")]
        private int _fromWave = 1;

        [SerializeField, Min(0f), Tooltip("1ウェーブ進むごとのHP増加率（0.1で+10%/ウェーブ）")]
        private float _hpScalePerWave = 0.1f;

        [SerializeField, Min(0f), Tooltip("1ウェーブ進むごとの攻撃力増加率（0.05で+5%/ウェーブ）")]
        private float _damageScalePerWave = 0.05f;

        public int FromWave => _fromWave;
        public float HpScalePerWave => _hpScalePerWave;
        public float DamageScalePerWave => _damageScalePerWave;
    }
}
