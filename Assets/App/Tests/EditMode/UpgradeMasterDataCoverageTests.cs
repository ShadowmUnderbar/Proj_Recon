using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Battle.DataStore;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Localization.Tables;

namespace App.Tests.EditMode
{
    /// <summary>
    /// アップグレードの「足したのに効かない・表示が壊れる」取りこぼしを、実アセットを読んで検出する。
    /// - enum に値があるのにマスターデータの行が無い（消費側だけ先に書いた・シートから消し忘れた）
    /// - マスターデータの NameKey に対してローカライズ表のキー（タイトル・簡略説明・詳細説明）が無い
    /// - 詳細説明に {valueN} があるのに UpgradeDescriptionFormatter が置き換えない（ValueFormats の登録漏れ）
    /// プロジェクトのアセットを直接読むため、インポート・ローカライズ表の取り込みを忘れると落ちる（落ちることが目的）
    /// </summary>
    public class UpgradeMasterDataCoverageTests
    {
        private const string DatabasePath = "Assets/App/MasterData/Database/UpgradeDatabase.asset";
        private const string SharedTablePath = "Assets/Localization/Upgrade Shared Data.asset";
        private const string JapaneseTablePath = "Assets/Localization/Upgrade_ja-JP.asset";
        private const string EnglishTablePath = "Assets/Localization/Upgrade_en.asset";

        // UpgradeLocalizationKey（internal）と同じ規則。変えるときは両方そろえる
        private const string SimpleDescriptionSuffix = "_SimpleDesc";
        private const string DescriptionSuffix = "_Desc";

        // {value1}〜{value5}
        private static readonly Regex ValuePlaceholder = new(@"\{value[1-5]\}", RegexOptions.Compiled);

        // enum にはあるがマスターデータの行を持たない既知の値。
        // 回避系 3 種はホップステップに置き換わった旧タイプで、シート側で消す予定。新しく増やさないこと
        private static readonly HashSet<UpgradeType> KnownRowlessTypes = new()
        {
            UpgradeType.None,
            UpgradeType.DodgeDistance,
            UpgradeType.DodgeCount,
            UpgradeType.DodgeCooldown,
        };

        private UpgradeDatabase _database;
        private SharedTableData _sharedTable;

        [SetUp]
        public void SetUp()
        {
            _database = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(DatabasePath);
            Assert.IsNotNull(_database, $"{DatabasePath} が読めない");
            Assert.IsNotNull(_database.UpgradeMasterData, "UpgradeDatabase の配列が null");

            _sharedTable = AssetDatabase.LoadAssetAtPath<SharedTableData>(SharedTablePath);
            Assert.IsNotNull(_sharedTable, $"{SharedTablePath} が読めない");
        }

        [Test]
        public void データベースにnullやID重複が無い()
        {
            var rows = _database.UpgradeMasterData;
            Assert.IsFalse(rows.Any(row => row == null), "UpgradeDatabase に null の参照がある（インポートし直す）");

            var duplicated = rows.GroupBy(row => row.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            Assert.IsEmpty(duplicated, "id が重複している: " + string.Join(", ", duplicated));
        }

        [Test]
        public void enumの全タイプにマスターデータの行がある()
        {
            var typesWithRows = _database.UpgradeMasterData.Select(row => row.UpgradeType).ToHashSet();
            var missing = Enum.GetValues(typeof(UpgradeType)).Cast<UpgradeType>()
                .Where(type => !KnownRowlessTypes.Contains(type) && !typesWithRows.Contains(type))
                .ToList();

            Assert.IsEmpty(missing,
                "UpgradeType にあるのに UpgradeData の行が無い（CSV を足してインポートするか、シートから消す）: "
                + string.Join(", ", missing));
        }

        [Test]
        public void 全NameKeyのタイトルと説明がローカライズ表にある()
        {
            var missing = new List<string>();
            foreach (var nameKey in _database.UpgradeMasterData.Select(row => row.NameKey).Distinct())
            {
                foreach (var key in new[] { nameKey, nameKey + SimpleDescriptionSuffix, nameKey + DescriptionSuffix })
                {
                    if (!_sharedTable.Contains(key))
                    {
                        missing.Add(key);
                    }
                }
            }

            Assert.IsEmpty(missing,
                "ローカライズ表（Upgrade テーブル）にキーが無い。ショップではキー文字列がそのまま出る。"
                + "スプレッドシートの Upgrade シートに3行を足して String Table へ取り込む: "
                + string.Join(", ", missing));
        }

        [TestCase(JapaneseTablePath)]
        [TestCase(EnglishTablePath)]
        public void 詳細説明のvalueプレースホルダーが全て置き換わる(string tablePath)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>(tablePath);
            Assert.IsNotNull(table, $"{tablePath} が読めない");

            var unresolved = new List<string>();
            foreach (var upgrade in _database.UpgradeMasterData)
            {
                var entry = table.GetEntry(upgrade.NameKey + DescriptionSuffix);
                if (entry == null || string.IsNullOrEmpty(entry.Value) || !ValuePlaceholder.IsMatch(entry.Value))
                {
                    continue;
                }

                var formatted = UpgradeDescriptionFormatter.Format(entry.Value, upgrade);
                var remaining = ValuePlaceholder.Matches(formatted).Cast<Match>().Select(match => match.Value).Distinct();
                if (remaining.Any())
                {
                    unresolved.Add($"{upgrade.NameKey} L{upgrade.Level} ({upgrade.UpgradeType}): {string.Join(" ", remaining)}");
                }
            }

            Assert.IsEmpty(unresolved,
                "詳細説明の {valueN} が置き換わらない。UpgradeDescriptionFormatter.ValueFormats にそのタイプの規則を足す: \n"
                + string.Join("\n", unresolved));
        }
    }
}
