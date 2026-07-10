using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    public static class UpgradeDataImporter
    {
        // パス定数
        private const string CsvPath = "Assets/App/MasterData/Origin/UpgradeData.csv";
        private const string OutputPath = "Assets/App/MasterData/Upgrade";
        private const string DatabasePath = "Assets/App/MasterData/Database/UpgradeDatabase.asset";
        private const string UpgradeCsvFileName = "UpgradeData.csv";
        private const string UpgradeTypeFileName = "UpgradeType.cs";
        private const string DestCsPath = "Assets/App/Script/Common/Data/UpgradeType.cs";

        // CSV列インデックス（GASエクスポーターのスキーマに対応）
        // ヘッダー: id,NameKey,SimpleDescriptionKey,DescriptionKey,UpgradeType,PlayerUnlockType,Level,
        //          Value1,Value1ParameterType,Value2,Value2ParameterType,Value3,Value3ParameterType,
        //          Value4,Value4ParameterType,Value5,Value5ParameterType
        private const int ColId = 0;
        private const int ColNameKey = 1;
        private const int ColUpgradeType = 2;
        private const int ColPlayerUnlockType = 3;
        private const int ColLevel = 4;
        private const int ColValue1 = 5;
        private const int ColValue1ParameterType = 6;
        private const int ColValue2 = 7;
        private const int ColValue2ParameterType = 8;
        private const int ColValue3 = 9;
        private const int ColValue3ParameterType = 10;
        private const int ColValue4 = 11;
        private const int ColValue4ParameterType = 12;
        private const int ColValue5 = 13;
        private const int ColValue5ParameterType = 14;
        private const int ColBuffId = 15; // 任意列（GrantBuff用。既存行は列ごと省略可）
        private const int MinColumnCount = 15;

        private static readonly BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tools/マスターデータ/UpgradeData ファイルコピー")]
        private static void CopyFilesFromFolder()
        {
            var selectedFolder = EditorUtility.OpenFolderPanel("インポート元フォルダを選択", "", "");
            if (string.IsNullOrEmpty(selectedFolder)) return;

            var csvFiles = Directory.GetFiles(selectedFolder, UpgradeCsvFileName, SearchOption.AllDirectories);
            var csFiles = Directory.GetFiles(selectedFolder, UpgradeTypeFileName, SearchOption.AllDirectories);

            if (csvFiles.Length == 0 || csFiles.Length == 0)
            {
                var missing = new System.Text.StringBuilder();
                if (csvFiles.Length == 0) missing.AppendLine($"・{UpgradeCsvFileName}");
                if (csFiles.Length == 0) missing.AppendLine($"・{UpgradeTypeFileName}");
                EditorUtility.DisplayDialog("エラー", $"以下のファイルが見つかりませんでした:\n{missing}", "OK");
                return;
            }

            var srcCsv = csvFiles[0];
            var srcCs = csFiles[0];
            var destCsAbsolute = Path.GetFullPath(DestCsPath);
            var upgradeTypeChanged = !File.Exists(destCsAbsolute) ||
                                     File.ReadAllText(srcCs) != File.ReadAllText(destCsAbsolute);

            try
            {
                File.Copy(srcCsv, Path.GetFullPath(CsvPath), overwrite: true);
                File.Copy(srcCs, destCsAbsolute, overwrite: true);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"ファイルのコピーに失敗しました:\n{e.Message}", "OK");
                return;
            }

            AssetDatabase.Refresh();

            if (upgradeTypeChanged)
            {
                EditorUtility.DisplayDialog(
                    "UpgradeType.cs を更新しました",
                    "UpgradeType.cs を更新しました。\nUnityの再コンパイル後に再度インポートを実行してください。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("コピー完了", "ファイルのコピーが完了しました。", "OK");
            }
        }

        [MenuItem("Tools/マスターデータ/UpgradeData CSVインポート")]
        private static void Import()
        {
            if (!File.Exists(CsvPath))
            {
                EditorUtility.DisplayDialog("エラー", $"CSVファイルが見つかりません:\n{CsvPath}", "OK");
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(CsvPath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"CSVファイルの読み込みに失敗しました:\n{e.Message}", "OK");
                return;
            }

            // ヘッダー行（Row0）をスキップし、空行を除いたデータ行を収集
            var dataLines = new List<string>();
            for (var i = 1; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Length > 0)
                {
                    dataLines.Add(trimmed);
                }
            }

            if (dataLines.Count == 0)
            {
                EditorUtility.DisplayDialog("警告", "CSVにデータ行がありません。インポートを中断します。", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputPath))
            {
                AssetDatabase.CreateFolder("Assets/App/MasterData", "Upgrade");
            }

            var errors = new List<string>();
            var importedAssets = new List<UpgradeMasterData>();
            var type = typeof(UpgradeMasterData);

            for (var lineIdx = 0; lineIdx < dataLines.Count; lineIdx++)
            {
                var cols = dataLines[lineIdx].Split(',');
                var rowNum = lineIdx + 2; // ヘッダーを1行目とした場合の行番号

                if (cols.Length < MinColumnCount)
                {
                    errors.Add($"Row{rowNum}: 列数不足 (期待: {MinColumnCount}以上, 実際: {cols.Length})");
                    continue;
                }

                var id = cols[ColId].Trim();
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"Row{rowNum}: id が空です");
                    continue;
                }

                if (!TryParseEnum<UpgradeType>(cols[ColUpgradeType].Trim(), rowNum, "UpgradeType", errors,
                        out var upgradeType))
                {
                    continue;
                }

                if (!TryParseEnum<PlayerUnlockType>(cols[ColPlayerUnlockType].Trim(), rowNum, "PlayerUnlockType",
                        errors, out var playerUnlockType))
                {
                    continue;
                }

                if (!int.TryParse(cols[ColLevel].Trim(), out var level))
                {
                    errors.Add($"Row{rowNum}: Level のパース失敗 \"{cols[ColLevel].Trim()}\"");
                    continue;
                }

                if (!TryParseFloat(cols[ColValue1].Trim(), rowNum, "Value1", errors, out var value1))
                {
                    continue;
                }

                if (!TryParseEnum<ParameterType>(cols[ColValue1ParameterType].Trim(), rowNum, "Value1ParameterType",
                        errors, out var value1Pt))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColValue2].Trim(), rowNum, "Value2", errors, out var value2))
                {
                    continue;
                }

                if (!TryParseEnum<ParameterType>(cols[ColValue2ParameterType].Trim(), rowNum, "Value2ParameterType",
                        errors, out var value2Pt))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColValue3].Trim(), rowNum, "Value3", errors, out var value3))
                {
                    continue;
                }

                if (!TryParseEnum<ParameterType>(cols[ColValue3ParameterType].Trim(), rowNum, "Value3ParameterType",
                        errors, out var value3Pt))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColValue4].Trim(), rowNum, "Value4", errors, out var value4))
                {
                    continue;
                }

                if (!TryParseEnum<ParameterType>(cols[ColValue4ParameterType].Trim(), rowNum, "Value4ParameterType",
                        errors, out var value4Pt))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColValue5].Trim(), rowNum, "Value5", errors, out var value5))
                {
                    continue;
                }

                if (!TryParseEnum<ParameterType>(cols[ColValue5ParameterType].Trim(), rowNum, "Value5ParameterType",
                        errors, out var value5Pt))
                {
                    continue;
                }

                // NameKey の $ を除いた名前をファイル名に使用
                var assetName = cols[ColNameKey].Trim().TrimStart('$');
                if (level >= 1)
                {
                    //かつレベルが1以上なら、ファイル名にレベルを付与して区別する
                    assetName += $"_L{level}";
                }
                var assetPath = $"{OutputPath}/{assetName}.asset";

                // 既存アセットを読み込む、なければ新規作成（同名なら上書き）
                var masterData = AssetDatabase.LoadAssetAtPath<UpgradeMasterData>(assetPath);
                if (masterData == null)
                {
                    masterData = ScriptableObject.CreateInstance<UpgradeMasterData>();
                    AssetDatabase.CreateAsset(masterData, assetPath);
                }

                // [SerializeField] がないためリフレクション経由でフィールドに値をセット
                type.GetField("_id", PrivateInstance).SetValue(masterData, id);
                type.GetField("_nameKey", PrivateInstance).SetValue(masterData, cols[ColNameKey].Trim());
                type.GetField("_upgradeType", PrivateInstance).SetValue(masterData, upgradeType);
                type.GetField("_playerUnlockType", PrivateInstance).SetValue(masterData, playerUnlockType);
                type.GetField("_level", PrivateInstance).SetValue(masterData, level);
                type.GetField("_value1", PrivateInstance).SetValue(masterData, value1);
                type.GetField("_value1ParameterType", PrivateInstance).SetValue(masterData, value1Pt);
                type.GetField("_value2", PrivateInstance).SetValue(masterData, value2);
                type.GetField("_value2ParameterType", PrivateInstance).SetValue(masterData, value2Pt);
                type.GetField("_value3", PrivateInstance).SetValue(masterData, value3);
                type.GetField("_value3ParameterType", PrivateInstance).SetValue(masterData, value3Pt);
                type.GetField("_value4", PrivateInstance).SetValue(masterData, value4);
                type.GetField("_value4ParameterType", PrivateInstance).SetValue(masterData, value4Pt);
                type.GetField("_value5", PrivateInstance).SetValue(masterData, value5);
                type.GetField("_value5ParameterType", PrivateInstance).SetValue(masterData, value5Pt);
                var buffId = cols.Length > ColBuffId ? cols[ColBuffId].Trim() : string.Empty;
                type.GetField("_buffId", PrivateInstance).SetValue(masterData, buffId);

                EditorUtility.SetDirty(masterData);
                importedAssets.Add(masterData);
            }

            // UpgradeDatabase の配列を全件置き換え
            var database = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogWarning($"[UpgradeDataImporter] UpgradeDatabase が見つかりません: {DatabasePath}");
            }
            else
            {
                var dbSo = new SerializedObject(database);
                var arrayProp = dbSo.FindProperty("_upgradeMasterData");
                arrayProp.arraySize = importedAssets.Count;
                for (int i = 0; i < importedAssets.Count; i++)
                    arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = importedAssets[i];
                dbSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"{importedAssets.Count} 件のアセットをインポートしました。";
            if (errors.Count > 0)
            {
                message += $"\n\n警告 ({errors.Count} 件):\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("インポート完了（警告あり）", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("インポート完了", message, "OK");
            }
        }

        private static bool TryParseEnum<T>(string raw, int rowNum, string colName, List<string> errors, out T result)
            where T : struct, Enum
        {
            if (Enum.TryParse<T>(raw, out result) && Enum.IsDefined(typeof(T), result))
                return true;
            errors.Add($"Row{rowNum}: {colName} のパース失敗 \"{raw}\"");
            return false;
        }

        private static bool TryParseFloat(string raw, int rowNum, string colName, List<string> errors, out float result)
        {
            if (float.TryParse(raw, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out result))
                return true;
            errors.Add($"Row{rowNum}: {colName} のパース失敗 \"{raw}\"");
            return false;
        }
    }
}