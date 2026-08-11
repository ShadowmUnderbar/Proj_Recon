using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 「一定時間内に何体を撃破／被弾させたか」を数えるための履歴バッファ。
    /// 同一の敵は何度記録しても1体として数える（爆風や貫通で同じ敵に複数回当たるため）。
    /// </summary>
    public class StreamerCameraMultiTargetTracker
    {
        private readonly struct Entry
        {
            public Entry(float time, int enemyId, Vector3 position)
            {
                Time = time;
                EnemyId = enemyId;
                Position = position;
            }

            public float Time { get; }
            public int EnemyId { get; }
            public Vector3 Position { get; }
        }

        // 履歴の上限。時間窓より長く保持する必要はないが、
        // Prune漏れで無限に伸びないよう上限を設けておく
        private const int MaxEntryCount = 64;

        private readonly List<Entry> _entries = new();

        public void Record(float time, int enemyId, Vector3 position)
        {
            _entries.Add(new Entry(time, enemyId, position));

            if (_entries.Count > MaxEntryCount)
            {
                _entries.RemoveAt(0);
            }
        }

        /// <summary>指定時刻より古い履歴を破棄する</summary>
        public void Prune(float oldestAllowedTime)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Time < oldestAllowedTime)
                {
                    _entries.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// windowStartTime以降の履歴から重複しない敵の座標を集め、requiredCount体以上いればtrueを返す。
        /// </summary>
        public bool TryCollectPositions(
            float windowStartTime,
            int requiredCount,
            out IReadOnlyList<Vector3> positions
        )
        {
            var collectedIds = new HashSet<int>();
            var collectedPositions = new List<Vector3>();

            foreach (var entry in _entries)
            {
                if (entry.Time < windowStartTime)
                {
                    continue;
                }

                if (!collectedIds.Add(entry.EnemyId))
                {
                    continue;
                }

                collectedPositions.Add(entry.Position);
            }

            positions = collectedPositions;
            return collectedPositions.Count >= requiredCount;
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
