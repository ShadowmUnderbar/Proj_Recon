using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// 回避中に残像を残すタイミングを、進んだ距離で決める。
    /// 時間ではなく距離で刻むので、回避が一時停止している間（フリーズ・時止め）は増えず、間隔も揃う。
    /// </summary>
    public class AfterimageSpawnScheduler
    {
        private Vector3 _lastSpawnPosition;
        private bool _isActive;

        /// <summary>回避の開始地点を起点にする。開始地点の1体は呼び出し側が残す</summary>
        public void Begin(Vector3 startPosition)
        {
            _lastSpawnPosition = startPosition;
            _isActive = true;
        }

        /// <summary>回避が終わったら止める。次の <see cref="Begin"/> まで残さない</summary>
        public void End()
        {
            _isActive = false;
        }

        /// <summary>前回残した地点から <paramref name="spawnDistance"/> 以上進んでいれば、この地点を起点に更新して true</summary>
        public bool Advance(Vector3 position, float spawnDistance)
        {
            if (!_isActive || spawnDistance <= 0f)
            {
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
