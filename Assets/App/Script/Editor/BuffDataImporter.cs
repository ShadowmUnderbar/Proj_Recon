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
    public static class BuffDataImporter
    {
        // パス定数
        private const string CsvPath = "Assets/App/MasterData/Origin/BuffData.csv";
        private const string OutputPath = "Assets/App/MasterData/Buff";
        private const string DatabasePath = "Assets/App/MasterData/Database/BuffDatabase.asset";

        // CSV列インデックス
        // ヘッダー: id,NameKey,ConditionType,ConditionValue,Duration,EffectType,EffectValue
        private const int ColId = 0;
        private const int ColNameKey = 1;
        private const int ColConditionType = 2;
        private const int ColConditionValue = 3;
        private const int ColDuration = 4;
        private const int ColEffectType = 5;
        private const int ColEffectValue = 6;
        private const int MinColumnCount = 7;

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
                AssetDatabase.CreateFolder("Assets/App/MasterData", "Buff");
            }

            var errors = new List<string>();
            var importedAssets = new List<BuffMasterData>();
            var type = typeof(BuffMasterData);

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

                if (!TryParseEnum<BuffConditionType>(cols[ColConditionType].Trim(), rowNum, "ConditionType", errors,
                        out var conditionType))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColConditionValue].Trim(), rowNum, "ConditionValue", errors,
                        out var conditionValue))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColDuration].Trim(), rowNum, "Duration", errors, out var duration))
                {
                    continue;
                }

                if (!TryParseEnum<BuffEffectType>(cols[ColEffectType].Trim(), rowNum, "EffectType", errors,
                        out var effectType))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColEffectValue].Trim(), rowNum, "EffectValue", errors, out var effectValue))
                {
                    continue;
                }

                // NameKey の $ を除いた名前をファイル名に使用
                var assetName = cols[ColNameKey].Trim().TrimStart('$');
                var assetPath = $"{OutputPath}/{assetName}.asset";

                // 既存アセットを読み込む、なければ新規作成（同名なら上書き）
                var masterData = AssetDatabase.LoadAssetAtPath<BuffMasterData>(assetPath);
                if (masterData == null)
                {
                    masterData = ScriptableObject.CreateInstance<BuffMasterData>();
                    AssetDatabase.CreateAsset(masterData, assetPath);
                }

                // ReadOnly の [SerializeField] へリフレクション経由で値をセット
                type.GetField("_id", PrivateInstance).SetValue(masterData, id);
                type.GetField("_nameKey", PrivateInstance).SetValue(masterData, cols[ColNameKey].Trim());
                type.GetField("_conditionType", PrivateInstance).SetValue(masterData, conditionType);
                type.GetField("_conditionValue", PrivateInstance).SetValue(masterData, conditionValue);
                type.GetField("_duration", PrivateInstance).SetValue(masterData, duration);
                type.GetField("_effectType", PrivateInstance).SetValue(masterData, effectType);
                type.GetField("_effectValue", PrivateInstance).SetValue(masterData, effectValue);

                EditorUtility.SetDirty(masterData);
                importedAssets.Add(masterData);
            }

            // BuffDatabase の配列を全件置き換え（無ければ新規作成）
            var database = AssetDatabase.LoadAssetAtPath<BuffDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<BuffDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var dbSo = new SerializedObject(database);
            var arrayProp = dbSo.FindProperty("_buffMasterData");
            arrayProp.arraySize = importedAssets.Count;
            for (int i = 0; i < importedAssets.Count; i++)
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = importedAssets[i];
            dbSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);

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
