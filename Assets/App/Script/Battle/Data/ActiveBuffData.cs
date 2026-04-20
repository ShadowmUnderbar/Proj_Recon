using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// バトル中に保持するバフのランタイム状態。
    /// スタック数は加算で管理し、効果時間は既存と新規の長い方を採用する。
    /// </summary>
    public class ActiveBuffData
    {
        public string BuffId { get; }
        public BuffType BuffType { get; }

        /// <summary>現在のスタック数。1以上MaxStack以下。</summary>
        public int StackCount { get; private set; }

        /// <summary>
        /// 残効果時間（秒）。全スタック共通。
        /// HasDuration=falseのバフでは0のまま（時間管理しない）。
        /// </summary>
        public float RemainingDuration { get; private set; }

        public bool HasDuration { get; }

        public ActiveBuffData(string buffId, BuffType buffType, float initialDuration, bool hasDuration, int initialStackCount = 1)
        {
            BuffId = buffId;
            BuffType = buffType;
            StackCount = initialStackCount;
            HasDuration = hasDuration;
            RemainingDuration = hasDuration ? initialDuration : 0f;
        }

        /// <summary>
        /// スタックを加算する。効果時間は既存残時間と新規時間の長い方を採用する。
        /// 例: 残10秒に3秒追加→10秒、残5秒に7秒追加→7秒
        /// </summary>
        public void AddStack(int count, float duration, bool hasDuration)
        {
            StackCount += count;
            if (hasDuration)
                RemainingDuration = Mathf.Max(RemainingDuration, duration);
        }

        /// <summary>
        /// 時間経過を反映する。HasDuration=falseのバフは変化なし。
        /// 時間切れで全スタック消滅し0を返す。
        /// </summary>
        public int Tick(float deltaTime)
        {
            if (!HasDuration) return StackCount;

            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0f)
            {
                RemainingDuration = 0f;
                StackCount = 0;
            }
            return StackCount;
        }

        /// <summary>スタックを1消費する。スタック消費発動型のバフ向け。</summary>
        public bool ConsumeStack()
        {
            if (StackCount <= 0) return false;
            StackCount--;
            return true;
        }
    }
}
