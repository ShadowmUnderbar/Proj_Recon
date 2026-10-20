using System.Collections.Generic;
using System.Linq;
using App.Battle.Data;
using App.Battle.DataStore;
using NUnit.Framework;

namespace App.Tests.EditMode
{
    /// <summary>
    /// ウイルス（VirusDataStore）の付与・重複・毎秒ダメージの単体テスト。
    /// 敵やシーンは使わず、時間を直接進めて出てくるダメージを見る。
    /// </summary>
    public class VirusDataStoreTests
    {
        private const int EnemyA = 1;
        private const int EnemyB = 2;
        private const float Tolerance = 1e-4f;

        private VirusDataStore _store;
        private List<VirusDamage> _buffer;

        [SetUp]
        public void SetUp()
        {
            _store = new VirusDataStore();
            _buffer = new List<VirusDamage>();
        }

        // 指定秒数を deltaTime 刻みで進め、発生したダメージをすべて返す
        private List<VirusDamage> Run(float seconds, float deltaTime)
        {
            var all = new List<VirusDamage>();
            var steps = RoundToInt(seconds / deltaTime);
            for (var i = 0; i < steps; i++)
            {
                _store.Advance(deltaTime, _buffer);
                all.AddRange(_buffer);
            }

            return all;
        }

        private static int RoundToInt(float value) => (int)System.Math.Round(value);

        [Test]
        public void 威力10効果時間5秒なら1秒ごとに2ダメージを5回与えて消える()
        {
            _store.Apply(EnemyA, 10f, 5f);

            var damages = Run(6f, 0.5f);

            Assert.AreEqual(5, damages.Count);
            Assert.That(damages.All(d => d.EnemyId == EnemyA && System.Math.Abs(d.Damage - 2f) < Tolerance));
            Assert.IsFalse(_store.TryGetVirus(EnemyA, out _));
        }

        [Test]
        public void 細かいフレームで進めても回数を取りこぼさない()
        {
            _store.Apply(EnemyA, 10f, 5f);

            var damages = Run(6f, 1f / 90f);

            Assert.AreEqual(5, damages.Count);
            Assert.AreEqual(10f, damages.Sum(d => d.Damage), Tolerance);
        }

        [Test]
        public void 新しい効果時間が残りより長ければ最長と残りを置き換え威力を加算する()
        {
            _store.Apply(EnemyA, 10f, 5f);
            Run(3f, 1f);

            _store.Apply(EnemyA, 3f, 3f);

            Assert.IsTrue(_store.TryGetVirus(EnemyA, out var status));
            Assert.AreEqual(13f, status.Power, Tolerance);
            Assert.AreEqual(3f, status.MaxDuration, Tolerance);
            Assert.AreEqual(3f, status.RemainingTime, Tolerance);
        }

        [Test]
        public void 新しい効果時間が残り以下なら時間はそのままで威力だけ加算する()
        {
            _store.Apply(EnemyA, 10f, 5f);
            Run(3f, 1f);

            _store.Apply(EnemyA, 2f, 1f);

            Assert.IsTrue(_store.TryGetVirus(EnemyA, out var status));
            Assert.AreEqual(12f, status.Power, Tolerance);
            Assert.AreEqual(5f, status.MaxDuration, Tolerance);
            Assert.AreEqual(2f, status.RemainingTime, Tolerance);
        }

        [Test]
        public void 重複後は加算した威力と新しい最長効果時間でダメージを計算する()
        {
            _store.Apply(EnemyA, 10f, 5f);
            Run(3f, 1f);
            _store.Apply(EnemyA, 3f, 3f);

            var damages = Run(4f, 1f);

            Assert.AreEqual(3, damages.Count);
            Assert.AreEqual(13f / 3f, damages[0].Damage, Tolerance);
        }

        [TestCase(2f, 5f, 0f)] // 0.4 → 0
        [TestCase(2.5f, 5f, 1f)] // 0.5 → 1
        [TestCase(3f, 5f, 1f)] // 0.6 → 1
        [TestCase(5f, 5f, 1f)] // 1.0 → 1
        [TestCase(13f, 3f, 13f / 3f)] // 1より大きければそのまま
        public void 毎秒ダメージは1以下なら整数に四捨五入する(float power, float duration, float expected)
        {
            Assert.AreEqual(expected, VirusDataStore.CalcDamagePerSecond(power, duration), Tolerance);
        }

        [Test]
        public void 毎秒ダメージが0に丸まるときはダメージを出さないが時間は進む()
        {
            _store.Apply(EnemyA, 2f, 5f);

            var damages = Run(6f, 1f);

            Assert.AreEqual(0, damages.Count);
            Assert.IsFalse(_store.TryGetVirus(EnemyA, out _));
        }

        [Test]
        public void 威力か効果時間が0以下なら付与しない()
        {
            _store.Apply(EnemyA, 0f, 5f);
            _store.Apply(EnemyB, 5f, 0f);

            Assert.IsFalse(_store.TryGetVirus(EnemyA, out _));
            Assert.IsFalse(_store.TryGetVirus(EnemyB, out _));
        }

        [Test]
        public void 付け直しても1秒周期を引き継ぐのでダメージが出る()
        {
            // 1秒のウイルスを0.9秒ごとに付け直す（周期が毎回戻るとダメージが一度も出ない）
            var damageCount = 0;
            for (var i = 0; i < 10; i++)
            {
                _store.Apply(EnemyA, 1f, 1f);
                damageCount += Run(0.9f, 0.1f).Count;
            }

            Assert.AreEqual(9, damageCount);
        }

        [Test]
        public void 敵ごとに独立して進む()
        {
            _store.Apply(EnemyA, 10f, 5f);
            Run(2f, 1f);
            _store.Apply(EnemyB, 4f, 2f);

            var damages = Run(2f, 1f);

            Assert.AreEqual(2, damages.Count(d => d.EnemyId == EnemyA));
            Assert.AreEqual(2, damages.Count(d => d.EnemyId == EnemyB));
            Assert.IsTrue(_store.TryGetVirus(EnemyA, out _));
            Assert.IsFalse(_store.TryGetVirus(EnemyB, out _));
        }

        [Test]
        public void 取り除いた敵とリセット後はウイルスが残らない()
        {
            _store.Apply(EnemyA, 10f, 5f);
            _store.Apply(EnemyB, 10f, 5f);

            _store.Remove(EnemyA);
            Assert.IsFalse(_store.TryGetVirus(EnemyA, out _));
            Assert.IsTrue(_store.TryGetVirus(EnemyB, out _));

            _store.ResetRun();
            Assert.IsFalse(_store.TryGetVirus(EnemyB, out _));
        }
    }
}
