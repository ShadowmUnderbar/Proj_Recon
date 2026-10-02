using System;

namespace App.Battle.Data
{
    /// <summary>
    /// ボスグループの1個体が行動を取れるかを判断するための状態。
    /// 既定値（default）は「まだ出現していない」を表し、行動不能として扱う。
    /// </summary>
    public readonly struct BossMemberStatus : IEquatable<BossMemberStatus>
    {
        public BossMemberStatus(bool isSpawned, BossActionPhase phase, bool isStun, bool isDead)
        {
            IsSpawned = isSpawned;
            Phase = phase;
            IsStun = isStun;
            IsDead = isDead;
        }

        /// <summary>出現済み（AIの初期化が済んでいる）か</summary>
        public bool IsSpawned { get; }

        public BossActionPhase Phase { get; }

        /// <summary>スタン中か（メデューサ・回避時跳ね返し）</summary>
        public bool IsStun { get; }

        public bool IsDead { get; }

        /// <summary>
        /// 次の行動を命令できるか。
        /// 行動中・攻撃後の硬直・スタン・撃破済みはいずれも行動不能とみなす。
        /// </summary>
        public bool IsActionable => IsSpawned && Phase == BossActionPhase.Ready && !IsStun && !IsDead;

        public bool Equals(BossMemberStatus other)
        {
            return IsSpawned == other.IsSpawned && Phase == other.Phase && IsStun == other.IsStun &&
                   IsDead == other.IsDead;
        }

        public override bool Equals(object obj)
        {
            return obj is BossMemberStatus other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsSpawned, Phase, IsStun, IsDead);
        }

        public override string ToString()
        {
            return $"Spawned:{IsSpawned} Phase:{Phase} Stun:{IsStun} Dead:{IsDead}";
        }
    }
}
