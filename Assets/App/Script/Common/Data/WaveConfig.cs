using System.Collections.Generic;
using UnityEngine;

namespace App.Common.Data
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Config/WaveConfig")]
    public class WaveConfig : ScriptableObject
    {
        [Header("ウェーブ進行条件 (OR条件のどちらか先に達成で進行)")]
        [SerializeField, Tooltip("1ウェーブの制限時間（秒）")]
        private float _waveDurationSeconds = 60f;

        [SerializeField, Tooltip("1ウェーブの目標撃破数")]
        private int _waveEnemyKillCount = 20;

        [Header("ウェーブ進行設定")]
        [SerializeField, Tooltip("最大ウェーブ数（0で無限ループ）")]
        private int _maxWaveCount = 0;

        [Header("ウェーブ難易度スケーリング")]
        [SerializeField, Tooltip("敵強化の増加率をウェーブ帯ごとに指定する。FromWaveの小さい順に区切られ、" +
                                 "ウェーブ1は等倍。空の場合は強化しない")]
        private List<EnemyWaveScalingTier> _enemyScalingTiers = new();

        public float WaveDurationSeconds => _waveDurationSeconds;
        public int WaveEnemyKillCount => _waveEnemyKillCount;
        public int MaxWaveCount => _maxWaveCount;
        public bool HasMaxWave => _maxWaveCount > 0;
        public IReadOnlyList<EnemyWaveScalingTier> EnemyScalingTiers => _enemyScalingTiers;
    }
}
