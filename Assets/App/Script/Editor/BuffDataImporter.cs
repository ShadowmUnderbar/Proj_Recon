using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    public static class BuffDataImporter
    {
        private const string CsvPath = "Assets/App/MasterData/Origin/BuffData.csv";
        private const string OutputPath = "Assets/App/MasterData/Buff";
        private const string DatabasePath = "Assets/App/MasterData/Database/BuffDatabase.asset";

        // CSV列インデックス
        // ヘッダー: id,NameKey,BuffType,Duration,BaseValue,MaxStack
        private const int ColId = 0;
        private const int ColNameKey = 1;
        private const int ColBuffType = 2;
        private const int ColDuration = 3;
        private const int ColBaseValue = 4;
        private const int ColMaxStack = 5;
        private const int MinColumnCount = 6;

        private static readonly BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tools/マスターデータ/BuffData CSVインポート")]
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

            // ヘッダー行をスキップし、空行を除いたデータ行を収集
            var dataLines = new List<string>();
            for (var i = 1; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Length > 0) dataLines.Add(trimmed);
            }

            if (dataLines.Count == 0)
            {
                EditorUtility.DisplayDialog("警告", "CSVにデータ行がありません。インポートを中断します。", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputPath))
                AssetDatabase.CreateFolder("Assets/App/MasterData", "Buff");

            var errors = new List<string>();
            var importedAssets = new List<BuffMasterData>();
            var type = typeof(BuffMasterData);

            for (var lineIdx = 0; lineIdx < dataLines.Count; lineIdx++)
            {
                var cols = dataLines[lineIdx].Split(',');
                var rowNum = lineIdx + 2;

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

                if (!TryParseEnum<BuffType>(cols[ColBuffType].Trim(), rowNum, "BuffType", errors, out var buffType))
                    continue;

                if (!TryParseFloat(cols[ColDuration].Trim(), rowNum, "Duration", errors, out var duration))
                    continue;

                if (!TryParseFloat(cols[ColBaseValue].Trim(), rowNum, "BaseValue", errors, out var baseValue))
                    continue;

                if (!int.TryParse(cols[ColMaxStack].Trim(), out var maxStack))
                {
                    errors.Add($"Row{rowNum}: MaxStack のパース失敗 \"{cols[ColMaxStack].Trim()}\"");
                    continue;
                }

                var assetName = cols[ColNameKey].Trim().TrimStart('$');
                var assetPath = $"{OutputPath}/{assetName}.asset";

                var masterData = AssetDatabase.LoadAssetAtPath<BuffMasterData>(assetPath);
                if (masterData == null)
                {
                    masterData = ScriptableObject.CreateInstance<BuffMasterData>();
                    AssetDatabase.CreateAsset(masterData, assetPath);
                }

                type.GetField("_id", PrivateInstance).SetValue(masterData, id);
                type.GetField("_nameKey", PrivateInstance).SetValue(masterData, cols[ColNameKey].Trim());
                type.GetField("_buffType", PrivateInstance).SetValue(masterData, buffType);
                type.GetField("_duration", PrivateInstance).SetValue(masterData, duration);
                type.GetField("_baseValue", PrivateInstance).SetValue(masterData, baseValue);
                type.GetField("_maxStack", PrivateInstance).SetValue(masterData, maxStack);

                EditorUtility.SetDirty(masterData);
                importedAssets.Add(masterData);
            }

            var database = AssetDatabase.LoadAssetAtPath<BuffDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogWarning($"[BuffDataImporter] BuffDatabase が見つかりません: {DatabasePath}");
            }
            else
            {
                var dbSo = new SerializedObject(database);
                var arrayProp = dbSo.FindProperty("_buffMasterData");
                arrayProp.arraySize = importedAssets.Count;
                for (var i = 0; i < importedAssets.Count; i++)
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
