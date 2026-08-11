using System.Collections.Generic;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// どのゲームイベントでどの演出ショットを再生するかの割り当て。
    /// ショット未設定のトリガーは発動しない。
    /// </summary>
    [CreateAssetMenu(fileName = "StreamerCameraTriggerConfig", menuName = "Config/StreamerCameraTriggerConfig")]
    public class StreamerCameraTriggerConfig : ScriptableObject
    {
        [Header("ウェーブ")]
        [SerializeField, Tooltip("ウェーブが進行した瞬間（クリア→次ウェーブ開始）のショット。被写体はプレイヤー")]
        private StreamerCameraShotData _waveAdvancedShot;

        [Header("ボス")]
        [SerializeField, Tooltip("ボス出現時のショット")]
        private StreamerCameraShotData _bossSpawnShot;

        [SerializeField, Tooltip("ボス撃破時のショット")]
        private StreamerCameraShotData _bossDeadShot;

        [SerializeField, Tooltip("ボス扱いする敵ランク")]
        private List<EnemyRankType> _bossRanks = new() { EnemyRankType.Boss };

        [Header("複数体同時（カットイン）")]
        [SerializeField, Tooltip("上から順に判定し、最初に成立したルールを採用する。体数の多い条件を上に置く")]
        private List<StreamerCameraMultiTargetRule> _multiTargetRules = new();

        public StreamerCameraShotData WaveAdvancedShot => _waveAdvancedShot;
        public StreamerCameraShotData BossSpawnShot => _bossSpawnShot;
        public StreamerCameraShotData BossDeadShot => _bossDeadShot;
        public IReadOnlyList<StreamerCameraMultiTargetRule> MultiTargetRules => _multiTargetRules;

        public bool IsBossRank(EnemyRankType rankType) => _bossRanks.Contains(rankType);
    }
}
