using System;
using System.Collections.Generic;
using System.IO;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// スプレッドシートから出力した UpgradeTagData.csv（NameKey とタグの組を1行ずつ持つ縦持ち）を取り込み、
    /// 同じ NameKey を持つ全レベルの UpgradeMasterData にタグ配列を書き込む。
    /// UpgradeData のインポート後にも自動で呼ばれる
    /// </summary>
    public static class UpgradeTagDataImporter
    {
        // パス定数
        private const string CsvPath = "Assets/App/MasterData/Origin/UpgradeTagData.csv";
        private const string DatabasePath = "Assets/App/MasterData/Database/UpgradeDatabase.asset";
        private const string TagCsvFileName = "UpgradeTagData.csv";
        private const string UpgradeTagFileName = "UpgradeTag.cs";
        private const string DestCsPath = "Assets/App/Script/Common/Data/UpgradeTag.cs";

        // CSV列インデックス
        // ヘッダー: NameKey,Tag
        private const int ColNameKey = 0;
        private const int ColTag = 1;
        private const int MinColumnCount = 2;

        private const string TagsPropertyName = "_tags";

        [MenuItem("Tools/マスターデータ/UpgradeTagData ファイルコピー")]
        private static void CopyFilesFromFolder()
        {
            var selectedFolder = EditorUtility.OpenFolderPanel("インポート元フォルダを選択", "", "");
            if (string.IsNullOrEmpty(selectedFolder)) return;

            var csvFiles = Directory.GetFiles(selectedFolder, TagCsvFileName, SearchOption.AllDirectories);
            if (csvFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("エラー", $"以下のファイルが見つかりませんでした:\n・{TagCsvFileName}", "OK");
                return;
            }

            // enum は追加・変更があるときだけ出力すればよいので任意
            var csFiles = Directory.GetFiles(selectedFolder, UpgradeTagFileName, SearchOption.AllDirectories);
            var destCsAbsolute = Path.GetFullPath(DestCsPath);
            var upgradeTagChanged = false;

            try
            {
                if (csFiles.Length > 0)
                {
                    upgradeTagChanged = !File.Exists(destCsAbsolute) ||
                                        File.ReadAllText(csFiles[0]) != File.ReadAllText(destCsAbsolute);
                    File.Copy(csFiles[0], destCsAbsolute, overwrite: true);
                }

                File.Copy(csvFiles[0], Path.GetFullPath(CsvPath), overwrite: true);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"ファイルのコピーに失敗しました:\n{e.Message}", "OK");
                return;
            }

            AssetDatabase.Refresh();

            if (upgradeTagChanged)
            {
                EditorUtility.DisplayDialog(
                    "UpgradeTag.cs を更新しました",
                    "UpgradeTag.cs を更新しました。\nUnityの再コンパイル後にインポートを実行してください。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("コピー完了", "ファイルのコピーが完了しました。", "OK");
            }
        }

        [MenuItem("Tools/マスターデータ/UpgradeTagData CSVインポート")]
        private static void ImportFromMenu()
        {
            var database = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(DatabasePath);
            if (database == null)
            {
                EditorUtility.DisplayDialog("エラー", $"UpgradeDatabase が見つかりません:\n{DatabasePath}", "OK");
                return;
            }

            var errors = new List<string>();
            var taggedCount = Apply(database.UpgradeMasterData, errors);
            AssetDatabase.SaveAssets();

            var message = $"{taggedCount} 件のアセットにタグを設定しました。";
            if (errors.Count > 0)
            {
                message += $"\n\n警告 ({errors.Count} 件):\n" + string.Join("\n", errors);
            }

            EditorUtility.DisplayDialog(errors.Count > 0 ? "インポート完了（警告あり）" : "インポート完了", message, "OK");
        }

        /// <summary>
        /// UpgradeTagData.csv を読み、渡されたアセットの Tags を書き換える（CSVに無いアップグレードは空にする）。
        /// 保存（SaveAssets）は呼び出し側で行う
        /// </summary>
        /// <returns>タグを1件以上設定したアセット数</returns>
        public static int Apply(IReadOnlyList<UpgradeMasterData> assets, List<string> errors)
        {
            if (!File.Exists(CsvPath))
            {
                errors.Add($"[UpgradeTag] CSVファイルが見つからないためタグを更新しませんでした: {CsvPath}");
                return 0;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(CsvPath);
            }
            catch (Exception e)
            {
                errors.Add($"[UpgradeTag] CSVファイルの読み込みに失敗しました: {e.Message}");
                return 0;
            }

            var tagsByNameKey = Parse(lines, errors);

            var knownNameKeys = new HashSet<string>();
            var taggedCount = 0;
            foreach (var asset in assets)
            {
                if (asset == null) continue;

                knownNameKeys.Add(asset.NameKey);
                var tags = tagsByNameKey.TryGetValue(asset.NameKey, out var list)
                    ? list
                    : new List<UpgradeTag>();
                if (tags.Count > 0)
                {
                    taggedCount++;
                }

                var so = new SerializedObject(asset);
                var tagsProp = so.FindProperty(TagsPropertyName);
                tagsProp.arraySize = tags.Count;
                for (var i = 0; i < tags.Count; i++)
                {
                    // enumValueIndex は定義順の添字なので、値をそのまま入れる intValue を使う
                    tagsProp.GetArrayElementAtIndex(i).intValue = (int)tags[i];
                }

                if (so.ApplyModifiedPropertiesWithoutUndo())
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            foreach (var nameKey in tagsByNameKey.Keys)
            {
                if (!knownNameKeys.Contains(nameKey))
                {
                    errors.Add($"[UpgradeTag] UpgradeData に無い NameKey です: \"{nameKey}\"");
                }
            }

            return taggedCount;
        }

        /// <summary>
        /// CSV行（先頭はヘッダー）を NameKey ごとのタグ一覧にまとめる。並びはCSVの行順
        /// </summary>
        private static Dictionary<string, List<UpgradeTag>> Parse(string[] lines, List<string> errors)
        {
            var result = new Dictionary<string, List<UpgradeTag>>();

            // ヘッダー行（Row0）をスキップ
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var rowNum = i + 1; // ヘッダーを1行目とした場合の行番号
                var cols = line.Split(',');
                if (cols.Length < MinColumnCount)
                {
                    errors.Add($"[UpgradeTag] Row{rowNum}: 列数不足 (期待: {MinColumnCount}以上, 実際: {cols.Length})");
                    continue;
                }

                var nameKey = cols[ColNameKey].Trim();
                if (string.IsNullOrEmpty(nameKey))
                {
                    errors.Add($"[UpgradeTag] Row{rowNum}: NameKey が空です");
                    continue;
                }

                // シートでは要素名（Barrier など）で書く運用。数値でも読めるようにしておく
                var tagText = cols[ColTag].Trim();
                if (!Enum.TryParse<UpgradeTag>(tagText, out var tag) || !Enum.IsDefined(typeof(UpgradeTag), tag) ||
                    tag == UpgradeTag.None)
                {
                    errors.Add($"[UpgradeTag] Row{rowNum}: Tag のパース失敗 \"{tagText}\"");
                    continue;
                }

                if (!result.TryGetValue(nameKey, out var tags))
                {
                    tags = new List<UpgradeTag>();
                    result.Add(nameKey, tags);
                }

                if (tags.Contains(tag))
                {
                    errors.Add($"[UpgradeTag] Row{rowNum}: {nameKey} に {tag} が重複しています");
                    continue;
                }

                tags.Add(tag);
            }

            return result;
        }
    }
}
