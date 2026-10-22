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
        /// <summary>残す理由</summary>
        public enum Trigger
        {
            /// <summary>残さない</summary>
            None,

            /// <summary>一定距離進んだ（残像は実体から離れていく）</summary>
            Distance,

            /// <summary>一定時間たった（立ち止まっていると実体と重なる）</summary>
            Interval,
        }

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
        /// この地点・この時点を起点に更新して残す理由を返す（両方満たしたら距離を優先）。どちらも 0 以下ならその条件では残さない。
        /// 前フレームから <paramref name="warpDistance"/> 以上飛んでいれば、残さずにこの地点・この時点を起点にする（0 以下ならワープとみなさない）
        /// </summary>
        public Trigger Advance(Vector3 position, float deltaTime, float spawnDistance, float spawnInterval, float warpDistance)
        {
            if (!_isActive)
            {
                return Trigger.None;
            }

            var frameDelta = position - _lastFramePosition;
            frameDelta.y = 0f;
            _lastFramePosition = position;
            if (warpDistance > 0f && frameDelta.sqrMagnitude >= warpDistance * warpDistance)
            {
                _lastSpawnPosition = position;
                _elapsedSinceSpawn = 0f;
                return Trigger.None;
            }

            _elapsedSinceSpawn += deltaTime;

            var delta = position - _lastSpawnPosition;
            delta.y = 0f;
            var trigger = Trigger.None;
            if (spawnDistance > 0f && delta.sqrMagnitude >= spawnDistance * spawnDistance)
            {
                trigger = Trigger.Distance;
            }
            else if (spawnInterval > 0f && _elapsedSinceSpawn >= spawnInterval)
            {
                trigger = Trigger.Interval;
            }

            if (trigger == Trigger.None)
            {
                return Trigger.None;
            }

            _lastSpawnPosition = position;
            _elapsedSinceSpawn = 0f;
            return trigger;
        }
    }
}
