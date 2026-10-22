using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 一時強化の発動中に残像を残すタイミングを、進んだ距離で決める。
    /// 時間ではなく距離で刻むので、立ち止まっている間（フリーズ・時止め・ショップを含む）は増えず、間隔も揃う。
    /// 1フレームで大きく飛んだ場合はワープとみなし、残さずにその地点から数え直す。
    /// </summary>
    public class AfterimageSpawnScheduler
    {
        private Vector3 _lastSpawnPosition;
        private Vector3 _lastFramePosition;
        private bool _isActive;

        /// <summary>刻んでいる最中か（<see cref="Begin"/> から <see cref="End"/> まで）</summary>
        public bool IsActive => _isActive;

        /// <summary>この地点を起点に刻み始める</summary>
        public void Begin(Vector3 startPosition)
        {
            _lastSpawnPosition = startPosition;
            _lastFramePosition = startPosition;
            _isActive = true;
        }

        /// <summary>止める。次の <see cref="Begin"/> まで残さない</summary>
        public void End()
        {
            _isActive = false;
        }

        /// <summary>
        /// 前回残した地点から <paramref name="spawnDistance"/> 以上進んでいれば、この地点を起点に更新して true。
        /// 前フレームから <paramref name="warpDistance"/> 以上飛んでいれば、残さずにこの地点を起点にする
        /// </summary>
        public bool Advance(Vector3 position, float spawnDistance, float warpDistance)
        {
            if (!_isActive || spawnDistance <= 0f)
            {
                return false;
            }

            var frameDelta = position - _lastFramePosition;
            frameDelta.y = 0f;
            _lastFramePosition = position;
            if (frameDelta.sqrMagnitude >= warpDistance * warpDistance)
            {
                _lastSpawnPosition = position;
                return false;
            }

            var delta = position - _lastSpawnPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude < spawnDistance * spawnDistance)
            {
                return false;
            }

            _lastSpawnPosition = position;
            return true;
        }
    }
}
