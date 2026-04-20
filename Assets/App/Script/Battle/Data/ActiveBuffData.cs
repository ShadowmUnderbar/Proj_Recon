using System.Collections.Generic;

namespace App.Battle.Data
{
    /// <summary>
    /// バトル中に保持するバフのランタイム状態。
    /// スタック数と残時間はそれぞれ独立して管理する。
    /// </summary>
    public class ActiveBuffData
    {
        public string BuffId { get; }
        public BuffType BuffType { get; }

        /// <summary>現在のスタック数。1以上MaxStack以下。</summary>
        public int StackCount { get; private set; }

        /// <summary>
        /// スタックごとの残効果時間（秒）のリスト。
        /// HasDuration=falseのバフでは空リストのまま。
        /// </summary>
        private readonly List<float> _remainingTimes = new();
        public IReadOnlyList<float> RemainingTimes => _remainingTimes;

        public ActiveBuffData(string buffId, BuffType buffType, float initialDuration, bool hasDuration)
        {
            BuffId = buffId;
            BuffType = buffType;
            StackCount = 1;
            if (hasDuration) _remainingTimes.Add(initialDuration);
        }

        /// <summary>スタックを追加する（MaxStackチェックはDataStore側で実施済み）。</summary>
        public void AddStack(float duration, bool hasDuration)
        {
            StackCount++;
            if (hasDuration) _remainingTimes.Add(duration);
        }

        /// <summary>
        /// 時間経過を反映し残スタック数を返す。
        /// 0を返した場合はDataStore側でエントリ除去する。
        /// HasDuration=falseのバフは常にStackCountをそのまま返す。
        /// </summary>
        public int Tick(float deltaTime)
        {
            for (var i = _remainingTimes.Count - 1; i >= 0; i--)
            {
                _remainingTimes[i] -= deltaTime;
                if (_remainingTimes[i] <= 0f)
                {
                    _remainingTimes.RemoveAt(i);
                    StackCount--;
                }
            }
            return StackCount;
        }

        /// <summary>スタックを1消費する。スタック消費発動型のバフ向け。</summary>
        public bool ConsumeStack()
        {
            if (StackCount <= 0) return false;
            StackCount--;
            if (_remainingTimes.Count > 0) _remainingTimes.RemoveAt(0);
            return true;
        }
    }
}
