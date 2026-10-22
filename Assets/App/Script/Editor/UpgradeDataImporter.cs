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

        // GAS出力からCSVと一緒にコピーするenumファイル（ファイル名, コピー先パス）。MasterDataImportDispatcher が参照する
        public static readonly (string fileName, string destPath)[] EnumCsFiles =
        {
            ("UpgradeType.cs", "Assets/App/Script/Common/Data/UpgradeType.cs"),
        };

        // CSV列インデックス（GASエクスポーターのスキーマに対応）
        // ヘッダー: id,NameKey,UpgradeType,PlayerUnlockType,Level,
        //          Value1,Value1ParameterType,Value2,Value2ParameterType,Value3,Value3ParameterType,
        //          Value4,Value4ParameterType,Value5,Value5ParameterType,BuffId,Cost
        // （説明文のキーは NameKey から "_SimpleDesc" / "_Desc" を付けて引くため列は無い）
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
        private const int ColCost = 16; // 任意列（ショップ購入コスト。未設定なら0＝無償扱い）
        private const int MinColumnCount = 15;

        private static readonly BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// UpgradeData.csv をインポートしてアセットと UpgradeDatabase を更新する。
        /// </summary>
        /// <param name="interactive">
        /// true: 完了/エラーをダイアログ表示（メニュー実行向け）。
        /// false: ダイアログを出さずログに出力する（issue駆動などの自動実行向け。メインスレッドをブロックしない）。
        /// </param>
        /// <returns>インポートを実行できたら true、前提エラーで中断したら false</returns>
        public static bool ImportData(bool interactive = false)
        {
            if (!File.Exists(CsvPath))
            {
                Notify(interactive, "エラー", $"CSVファイルが見つかりません:\n{CsvPath}", isError: true);
                return false;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(CsvPath);
            }
            catch (Exception e)
            {
                Notify(interactive, "エラー", $"CSVファイルの読み込みに失敗しました:\n{e.Message}", isError: true);
                return false;
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
                Notify(interactive, "警告", "CSVにデータ行がありません。インポートを中断します。", isError: true);
                return false;
            }

            if (!AssetDatabase.IsValidFolder(OutputPath))
            {
                AssetDatabase.CreateFolder("Assets/App/MasterData", "Upgrade");
            }

            var errors = new List<string>();
            var importedAssets = new List<UpgradeMasterData>();
            var type = typeof(UpgradeMasterData);

            // 重複検出用。id は実行時の所持管理キー、NameKey+Level はアセット名なので、どちらも1行に限る
            var seenIds = new HashSet<string>();
            var seenAssetNames = new HashSet<string>();

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

                if (!seenIds.Add(id))
                {
                    errors.Add($"Row{rowNum}: id \"{id}\" が重複しています");
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

                // アセット名（_L{Level}）と「最高レベルのみ採用」の判定が 1 始まりを前提にしている
                if (level < 1)
                {
                    errors.Add($"Row{rowNum}: Level は 1 以上にしてください（実際: {level}）");
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

                // Cost は任意列（列ごと無い・空なら0＝無償）。値があるのに読めない行は取り込まない
                var cost = 0;
                var costText = cols.Length > ColCost ? cols[ColCost].Trim() : string.Empty;
                if (!string.IsNullOrEmpty(costText) && !int.TryParse(costText, out cost))
                {
                    errors.Add($"Row{rowNum}: Cost のパース失敗 \"{costText}\"");
                    continue;
                }

                // GrantBuff は BuffId が無いと実行時に何も起きない（警告だけ出る）ので、取り込み時に弾く
                var buffId = cols.Length > ColBuffId ? cols[ColBuffId].Trim() : string.Empty;
                if (upgradeType == UpgradeType.GrantBuff && string.IsNullOrEmpty(buffId))
                {
                    errors.Add($"Row{rowNum}: UpgradeType が GrantBuff なのに BuffId が空です");
                    continue;
                }

                // NameKey の $ を除いた名前にレベルを付けてファイル名にする（例: BaseDamageUp_L1）
                var assetName = $"{cols[ColNameKey].Trim().TrimStart('$')}_L{level}";
                if (!seenAssetNames.Add(assetName))
                {
                    errors.Add($"Row{rowNum}: NameKey と Level の組み合わせ \"{assetName}\" が重複しています");
                    continue;
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
                type.GetField("_buffId", PrivateInstance).SetValue(masterData, buffId);

                type.GetField("_cost", PrivateInstance).SetValue(masterData, cost);

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

            // 新規作成したアセットにもタグが入るよう、本体の取り込み後にタグを反映する
            var taggedCount = UpgradeTagDataImporter.Apply(importedAssets, errors);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var message = $"{importedAssets.Count} 件のアセットをインポートしました（タグ設定 {taggedCount} 件）。";
            if (errors.Count > 0)
            {
                message += $"\n\n警告 ({errors.Count} 件):\n" + string.Join("\n", errors);
                Notify(interactive, "インポート完了（警告あり）", message, isError: true);
            }
            else
            {
                Notify(interactive, "インポート完了", message, isError: false);
            }

            return true;
        }

        /// <summary>
        /// 対話モードならダイアログ、自動実行モードならログで結果を通知する。
        /// </summary>
        private static void Notify(bool interactive, string title, string message, bool isError)
        {
            if (interactive)
            {
                EditorUtility.DisplayDialog(title, message, "OK");
            }
            else if (isError)
            {
                Debug.LogError($"[UpgradeDataImporter] {title}: {message}");
            }
            else
            {
                Debug.Log($"[UpgradeDataImporter] {title}: {message}");
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