using System.Collections.Generic;
using App.Common.Views;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// 敵ごとの判定球（<see cref="EnemyGazeBoundsView"/>）と視線の距離を、決まった数ずつ順番に更新する（plain C#）。
    /// 全員を毎フレーム判定しないのは、敵が増えても1フレームの負担を一定に保つため。
    /// 更新が回ってくるまでは前回の距離を使うので、敵が多いほど反応は遅れる（1周 = 敵の数 ÷ 1フレームの判定数 フレーム）。
    /// 物理クエリは使わず、球と線分の距離計算だけで済ませる
    /// </summary>
    public class EnemyGazeTracker
    {
        /// <summary>敵ID と視線からの距離。まだ判定していない敵は無限大（どの半径にも入らない）</summary>
        private readonly List<(int enemyId, float distanceFromRay)> _distances = new();

        /// <summary>_distances と同じ並びの判定球</summary>
        private readonly List<EnemyGazeBoundsView> _bounds = new();

        /// <summary>次に判定する敵の位置</summary>
        private int _nextIndex;

        /// <summary>登録中の全敵の、最後に判定した時点の視線からの距離</summary>
        public IReadOnlyList<(int enemyId, float distanceFromRay)> Distances => _distances;

        public void Add(int enemyId, EnemyGazeBoundsView bounds)
        {
            _distances.Add((enemyId, float.PositiveInfinity));
            _bounds.Add(bounds);
        }

        public void Remove(int enemyId)
        {
            var index = FindIndex(enemyId);
            if (index < 0)
            {
                return;
            }

            _distances.RemoveAt(index);
            _bounds.RemoveAt(index);

            // 手前が抜けたぶん詰めて、順番を飛ばさないようにする
            if (index < _nextIndex)
            {
                _nextIndex--;
            }
        }

        public void Clear()
        {
            _distances.Clear();
            _bounds.Clear();
            _nextIndex = 0;
        }

        /// <summary>次の count 体について、視線（頭から maxDistance までの線分）からの距離を更新する</summary>
        public void EvaluateNext(in Pose gaze, int count, float maxDistance)
        {
            count = Mathf.Min(count, _distances.Count);
            for (var i = 0; i < count; i++)
            {
                if (_nextIndex >= _distances.Count)
                {
                    _nextIndex = 0;
                }

                var bounds = _bounds[_nextIndex];
                var enemyId = _distances[_nextIndex].enemyId;

                // 破棄済み（Destroy 済みで登録解除前）の敵は判定から外しておく
                var distance = bounds != null
                    ? GazeHitTest.DistanceFromGazeSegment(gaze, bounds.Center, bounds.Radius, maxDistance)
                    : float.PositiveInfinity;

                _distances[_nextIndex] = (enemyId, distance);
                _nextIndex++;
            }
        }

        private int FindIndex(int enemyId)
        {
            for (var i = 0; i < _distances.Count; i++)
            {
                if (_distances[i].enemyId == enemyId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
