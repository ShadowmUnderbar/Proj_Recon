using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;

namespace App.Battle.DataStore
{
    public class StreamerCameraDataStore : IStreamerCameraDataStore
    {
        private readonly ReactiveProperty<bool> _isPlaying = new(false);
        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        private readonly StreamerCameraMultiTargetTracker _killTracker = new();
        private readonly StreamerCameraMultiTargetTracker _damageTracker = new();

        /// <summary>ショットIDごとの「次に再生できる時刻」</summary>
        private readonly Dictionary<string, float> _nextPlayableTimes = new();

        /// <summary>履歴を保持する最大秒数。どのルールの時間窓よりも長く取っておく</summary>
        private const float HistoryRetentionSeconds = 5f;

        private float _elapsedTime;
        private int _playingPriority;

        public void AddElapsedTime(float deltaTime)
        {
            _elapsedTime += deltaTime;

            var oldestAllowedTime = _elapsedTime - HistoryRetentionSeconds;
            _killTracker.Prune(oldestAllowedTime);
            _damageTracker.Prune(oldestAllowedTime);
        }

        public void RecordKill(int enemyId, Vector3 position)
        {
            _killTracker.Record(_elapsedTime, enemyId, position);
        }

        public void RecordDamage(int enemyId, Vector3 position)
        {
            _damageTracker.Record(_elapsedTime, enemyId, position);
        }

        public bool TryCollectMultiTargetPositions(
            StreamerCameraMultiTargetRule rule,
            out IReadOnlyList<Vector3> subjectPositions
        )
        {
            var tracker = GetTracker(rule.CountType);
            var windowStartTime = _elapsedTime - rule.WindowSeconds;

            return tracker.TryCollectPositions(windowStartTime, rule.TargetCount, out subjectPositions);
        }

        public void ClearMultiTargetHistory(StreamerCameraMultiTargetCountType countType)
        {
            GetTracker(countType).Clear();
        }

        public bool CanPlay(StreamerCameraShotData shotData)
        {
            if (shotData == null)
            {
                return false;
            }

            // 再生中は、より高い優先度のショットのみ割り込める
            if (_isPlaying.CurrentValue && shotData.Priority <= _playingPriority)
            {
                return false;
            }

            if (_nextPlayableTimes.TryGetValue(shotData.ShotId, out var nextPlayableTime)
                && _elapsedTime < nextPlayableTime)
            {
                return false;
            }

            return true;
        }

        public void BeginShot(StreamerCameraShotData shotData)
        {
            _playingPriority = shotData.Priority;

            // クールダウンは演出の終わりを起点にする（尺の長いショットが連発しないように）
            _nextPlayableTimes[shotData.ShotId] = _elapsedTime + shotData.TotalDuration + shotData.CooldownSeconds;

            _isPlaying.Value = true;
        }

        public void EndShot()
        {
            _playingPriority = 0;
            _isPlaying.Value = false;
        }

        private StreamerCameraMultiTargetTracker GetTracker(StreamerCameraMultiTargetCountType countType) =>
            countType == StreamerCameraMultiTargetCountType.Kill ? _killTracker : _damageTracker;
    }
}
