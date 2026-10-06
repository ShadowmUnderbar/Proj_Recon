using System.Collections.Generic;
using System.Reflection;
using App.Battle.Views;
using App.Common.Data;
using App.Common.Data.MasterData;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests.EditMode
{
    /// <summary>
    /// アップグレードのタグ表示に使う計算（所持タグの集計・タグを流す表示）の単体テスト
    /// </summary>
    public class UpgradeTagTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _createdObjects)
            {
                Object.DestroyImmediate(created);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void 集計_レベル違いの同じアップグレードは1件と数える()
        {
            var upgrades = new[]
            {
                CreateUpgrade("$Rage", UpgradeTag.Rage, UpgradeTag.Hurt),
                CreateUpgrade("$Rage", UpgradeTag.Rage, UpgradeTag.Hurt),
                CreateUpgrade("$Adrenalin", UpgradeTag.Hurt),
            };

            var ranking = UpgradeTagRanking.Top(upgrades, 5);

            Assert.That(ranking.Count, Is.EqualTo(2));
            Assert.That(ranking[0].Tag, Is.EqualTo(UpgradeTag.Hurt));
            Assert.That(ranking[0].Count, Is.EqualTo(2));
            Assert.That(ranking[1].Tag, Is.EqualTo(UpgradeTag.Rage));
            Assert.That(ranking[1].Count, Is.EqualTo(1));
        }

        [Test]
        public void 集計_同数のタグはenumの定義順に並び上限件数で切る()
        {
            var upgrades = new[]
            {
                CreateUpgrade("$A", UpgradeTag.Critical),
                CreateUpgrade("$B", UpgradeTag.Barrier),
                CreateUpgrade("$C", UpgradeTag.Gaze),
            };

            var ranking = UpgradeTagRanking.Top(upgrades, 2);

            Assert.That(ranking.Count, Is.EqualTo(2));
            Assert.That(ranking[0].Tag, Is.EqualTo(UpgradeTag.Barrier));
            Assert.That(ranking[1].Tag, Is.EqualTo(UpgradeTag.Gaze));
        }

        [Test]
        public void 集計_タグなしやnullは数えない()
        {
            var upgrades = new[] { CreateUpgrade("$BaseDamageUp"), null };

            Assert.That(UpgradeTagRanking.Top(upgrades, 5), Is.Empty);
        }

        [Test]
        public void 流す表示_先頭で止まってから進み1周で先頭に戻る()
        {
            var scroller = new TagTickerScroller(speed: 2f, loopPause: 1f);
            scroller.Reset(cycleWidth: 10f);

            scroller.Advance(0.5f);
            Assert.That(scroller.Offset, Is.EqualTo(0f), "止まっている間は進まない");

            scroller.Advance(1f);
            Assert.That(scroller.Offset, Is.EqualTo(1f).Within(1e-4f), "止まる時間を使い切った残り0.5秒ぶん進む");

            scroller.Advance(5f);
            Assert.That(scroller.Offset, Is.EqualTo(0f), "1周の幅に達したら先頭に戻る");

            scroller.Advance(0.5f);
            Assert.That(scroller.Offset, Is.EqualTo(0f), "先頭に戻ったらまた止まる");
        }

        [Test]
        public void 流す表示_端に近い文字ほど薄く範囲外は消える()
        {
            Assert.That(TagTickerScroller.EdgeAlpha(-6f, -5f, 5f, 1f), Is.EqualTo(0f));
            Assert.That(TagTickerScroller.EdgeAlpha(-4.5f, -5f, 5f, 1f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(TagTickerScroller.EdgeAlpha(0f, -5f, 5f, 1f), Is.EqualTo(1f));
            Assert.That(TagTickerScroller.EdgeAlpha(4.9f, -5f, 5f, 0f), Is.EqualTo(1f), "フェード幅0なら範囲内はそのまま");
        }

        private UpgradeMasterData CreateUpgrade(string nameKey, params UpgradeTag[] tags)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeMasterData>();
            _createdObjects.Add(upgrade);

            var type = typeof(UpgradeMasterData);
            type.GetField("_nameKey", PrivateInstance).SetValue(upgrade, nameKey);
            type.GetField("_tags", PrivateInstance).SetValue(upgrade, tags);
            return upgrade;
        }
    }
}
