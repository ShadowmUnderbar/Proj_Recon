using System.Collections.Generic;
using System.Reflection;
using App.Battle.DataStore;
using App.Battle.Interface.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace App.Tests.EditMode
{
    /// <summary>
    /// アップグレード効果の計算基盤（UpgradeEffectSimpleCalculatorDataStore）と
    /// UpgradeDatabase の ID 引きの単体テスト。
    /// 所持内容の変化（Version が進む）で集計が作り直されることを中心に見る。
    /// </summary>
    public class UpgradeEffectSimpleCalculatorDataStoreTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float Tolerance = 1e-4f;

        // 所持アップグレードを直接差し替えられる最小限の実装
        private sealed class FakeSession : IUpgradeSessionDataStore
        {
            public readonly List<string> Ids = new();
            public readonly Subject<Unit> Changed = new();

            public IReadOnlyList<string> AppliedUpgrades => Ids;
            public Observable<Unit> OnChanged => Changed;
            public int Version { get; private set; }
            public IReadOnlyList<string> NewlyAcquiredUpgrades => Ids;

            public void AddUpgrade(UpgradeMasterData upgradeData)
            {
                Ids.Add(upgradeData.Id);
                NotifyChanged();
            }

            public void NotifyChanged()
            {
                Version++;
                Changed.OnNext(Unit.Default);
            }

            public void Preload(UpgradeMasterData upgradeData) => AddUpgrade(upgradeData);
        }

        private readonly List<Object> _createdObjects = new();
        private FakeSession _session;
        private UpgradeDatabase _database;
        private UpgradeEffectSimpleCalculatorDataStore _calculator;

        private UpgradeMasterData _damageL1;
        private UpgradeMasterData _damageL2;
        private UpgradeMasterData _barrierL1;
        private UpgradeMasterData _barrierL2;

        [SetUp]
        public void SetUp()
        {
            _damageL1 = CreateUpgrade("1", UpgradeType.BulletDamage, 1, 1.1f);
            _damageL2 = CreateUpgrade("2", UpgradeType.BulletDamage, 2, 1.2f);
            _barrierL1 = CreateUpgrade("3", UpgradeType.Barrier, 1, 0.3f);
            _barrierL2 = CreateUpgrade("4", UpgradeType.Barrier, 2, 0.5f);

            _database = ScriptableObject.CreateInstance<UpgradeDatabase>();
            _createdObjects.Add(_database);
            typeof(UpgradeDatabase).GetField("_upgradeMasterData", PrivateInstance)
                .SetValue(_database, new[] { _damageL1, _damageL2, _barrierL1, _barrierL2 });

            _session = new FakeSession();
            _calculator = new UpgradeEffectSimpleCalculatorDataStore(_session, _database);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                Object.DestroyImmediate(obj);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void 未所持なら乗算は1で最大値と最高レベルは0()
        {
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_calculator.CalcMax(UpgradeType.Barrier), Is.EqualTo(0f).Within(Tolerance));
            Assert.AreEqual(0, _calculator.CalcMaxLevel(UpgradeType.Barrier));
            Assert.IsFalse(_calculator.TryGetHighestLevelUpgrade(UpgradeType.Barrier, out _));
        }

        [Test]
        public void 同じ種類を2つ持つと乗算は積で最大値と最高レベルは高い方()
        {
            _session.AddUpgrade(_damageL1);
            _session.AddUpgrade(_damageL2);
            _session.AddUpgrade(_barrierL2);
            _session.AddUpgrade(_barrierL1);

            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f * 1.2f).Within(Tolerance));
            Assert.That(_calculator.CalcMax(UpgradeType.Barrier), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.AreEqual(2, _calculator.CalcMaxLevel(UpgradeType.Barrier));
            Assert.IsTrue(_calculator.TryGetHighestLevelUpgrade(UpgradeType.Barrier, out var highest));
            Assert.AreSame(_barrierL2, highest);
        }

        [Test]
        public void 所持が変わると集計が作り直される()
        {
            _session.AddUpgrade(_damageL1);
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f).Within(Tolerance));

            _session.AddUpgrade(_damageL2);
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f * 1.2f).Within(Tolerance));

            // ランのリセット相当: 所持を空にして通知する
            _session.Ids.Clear();
            _session.NotifyChanged();
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void 世代が進まない書き換えは前の集計を返し世代が進むと反映される()
        {
            _session.AddUpgrade(_damageL1);
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f).Within(Tolerance));

            // 世代番号が変わらない変更は反映されない（集計はキャッシュされている）
            _session.Ids.Add(_damageL2.Id);
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f).Within(Tolerance));

            _session.NotifyChanged();
            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f * 1.2f).Within(Tolerance));
        }

        [Test]
        public void データベースに無いIDは無視する()
        {
            _session.Ids.Add("999");
            _session.Ids.Add(_damageL1.Id);
            _session.NotifyChanged();

            Assert.That(_calculator.CalcMultiply(UpgradeType.BulletDamage), Is.EqualTo(1.1f).Within(Tolerance));
        }

        [Test]
        public void データベースのID引きは空や未登録でfalse()
        {
            Assert.IsTrue(_database.TryGetUpgradeMasterData("2", out var found));
            Assert.AreSame(_damageL2, found);
            Assert.IsFalse(_database.TryGetUpgradeMasterData("999", out _));
            Assert.IsFalse(_database.TryGetUpgradeMasterData(string.Empty, out _));
            Assert.IsFalse(_database.TryGetUpgradeMasterData(null, out _));
        }

        private UpgradeMasterData CreateUpgrade(string id, UpgradeType upgradeType, int level, float value1)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeMasterData>();
            _createdObjects.Add(upgrade);

            var type = typeof(UpgradeMasterData);
            type.GetField("_id", PrivateInstance).SetValue(upgrade, id);
            type.GetField("_upgradeType", PrivateInstance).SetValue(upgrade, upgradeType);
            type.GetField("_level", PrivateInstance).SetValue(upgrade, level);
            type.GetField("_value1", PrivateInstance).SetValue(upgrade, value1);
            return upgrade;
        }
    }
}
