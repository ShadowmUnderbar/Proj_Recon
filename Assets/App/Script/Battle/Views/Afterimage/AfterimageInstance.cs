using System;
using App.Battle.Data;

namespace App.Battle.Views.Afterimage
{
    /// <summary>
    /// 残像1体。形状（<see cref="AfterimageSnapshot"/>）と見た目、生成からの経過時間を持つ。
    /// 寿命が尽きたら形状を空にし、オブジェクトごと次の残像へ使い回す。
    /// </summary>
    public sealed class AfterimageInstance : IDisposable
    {
        public AfterimageSnapshot Snapshot { get; } = new();
        public AfterimageStyle Style { get; set; }

        /// <summary>生成からの経過秒数</summary>
        public float Age { get; private set; }

        public float Lifetime { get; private set; }

        /// <summary>欠損模様の乱数の種。残像ごとに模様を変える</summary>
        public uint Seed { get; private set; }

        public bool IsAlive => !Snapshot.IsEmpty && Age < Lifetime;

        /// <summary>寿命の進み（0〜1）</summary>
        public float LifeRatio => Lifetime > 0f ? Math.Min(Age / Lifetime, 1f) : 1f;

        /// <summary>写し取った直後に呼ぶ。経過時間を戻して寿命を始める</summary>
        public void Begin(AfterimageStyle style, float lifetime, uint seed)
        {
            Style = style;
            Lifetime = lifetime;
            Seed = seed;
            Age = 0f;
        }

        /// <summary>時間を進める。寿命が尽きたら形状を手放して false</summary>
        public bool Tick(float deltaTime)
        {
            if (Snapshot.IsEmpty)
            {
                return false;
            }

            Age += deltaTime;
            if (Age < Lifetime)
            {
                return true;
            }

            Snapshot.Clear();
            return false;
        }

        public void Dispose()
        {
            Snapshot.Dispose();
        }
    }
}
