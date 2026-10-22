using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 一時強化の発動中に残像を残すタイミングを、進んだ距離と経過時間で決める。
    /// 前回残してから一定距離進むか、一定時間たつか、どちらか先に満たした方で1体残し、両方を数え直す。
    /// 経過時間は呼び出し側が渡す分だけ進むので、止めたい間（ショップ・フリーズ・時止め）は 0 を渡す。
    /// 1フレームで大きく飛んだ場合はワープとみなし、残さずにその地点から数え直す。
    /// </summary>
    public class AfterimageSpawnScheduler
    {
        private Vector3 _lastSpawnPosition;
        private Vector3 _lastFramePosition;
        private float _elapsedSinceSpawn;
        private bool _isActive;

        /// <summary>刻んでいる最中か（<see cref="Begin"/> から <see cref="End"/> まで）</summary>
        public bool IsActive => _isActive;

        /// <summary>この地点・この時点を起点に刻み始める</summary>
        public void Begin(Vector3 startPosition)
        {
            _lastSpawnPosition = startPosition;
            _lastFramePosition = startPosition;
            _elapsedSinceSpawn = 0f;
            _isActive = true;
        }

        /// <summary>止める。次の <see cref="Begin"/> まで残さない</summary>
        public void End()
        {
            _isActive = false;
        }

        /// <summary>
        /// 前回残してから <paramref name="spawnDistance"/> 以上進んだか、<paramref name="spawnInterval"/> 秒以上たっていれば、
        /// この地点・この時点を起点に更新して true。どちらも 0 以下ならその条件では残さない。
        /// 前フレームから <paramref name="warpDistance"/> 以上飛んでいれば、残さずにこの地点・この時点を起点にする（0 以下ならワープとみなさない）
        /// </summary>
        public bool Advance(Vector3 position, float deltaTime, float spawnDistance, float spawnInterval, float warpDistance)
        {
            if (!_isActive)
            {
                return false;
            }

            var frameDelta = position - _lastFramePosition;
            frameDelta.y = 0f;
            _lastFramePosition = position;
            if (warpDistance > 0f && frameDelta.sqrMagnitude >= warpDistance * warpDistance)
            {
                _lastSpawnPosition = position;
                _elapsedSinceSpawn = 0f;
                return false;
            }

            _elapsedSinceSpawn += deltaTime;

            var delta = position - _lastSpawnPosition;
            delta.y = 0f;
            var isFarEnough = spawnDistance > 0f && delta.sqrMagnitude >= spawnDistance * spawnDistance;
            var isLongEnough = spawnInterval > 0f && _elapsedSinceSpawn >= spawnInterval;
            if (!isFarEnough && !isLongEnough)
            {
                return false;
            }

            _lastSpawnPosition = position;
            _elapsedSinceSpawn = 0f;
            return true;
        }
    }
}
