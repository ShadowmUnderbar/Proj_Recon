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
    /// <summary>
    /// スプレッドシートから出力した WaveScalingData.csv を取り込み、
    /// WaveScalingMasterData アセットと WaveScalingDatabase を更新する。
    /// </summary>
    public static class WaveScalingDataImporter
    {
        // パス定数
        private const string CsvPath = "Assets/App/MasterData/Origin/WaveScalingData.csv";
        private const string OutputPath = "Assets/App/MasterData/WaveScaling";
        private const string DatabasePath = "Assets/App/MasterData/Database/WaveScalingDatabase.asset";
        private const string WaveScalingCsvFileName = "WaveScalingData.csv";

        // CSV列インデックス
        // ヘッダー: id,wave,atkBuff,hpBuff
        private const int ColId = 0;
        private const int ColWave = 1;
        private const int ColAtkBuff = 2;
        private const int ColHpBuff = 3;
        private const int MinColumnCount = 4;

        private static readonly BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Tools/マスターデータ/WaveScalingData ファイルコピー")]
        private static void CopyFilesFromFolder()
        {
            var selectedFolder = EditorUtility.OpenFolderPanel("インポート元フォルダを選択", "", "");
            if (string.IsNullOrEmpty(selectedFolder)) return;

            string errorMessage;
            try
            {
                errorMessage = CopyFilesCore(selectedFolder);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"ファイルのコピーに失敗しました:\n{e.Message}", "OK");
                return;
            }

            if (errorMessage != null)
            {
                EditorUtility.DisplayDialog("エラー", errorMessage, "OK");
                return;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("コピー完了", "ファイルのコピーが完了しました。", "OK");
        }

        /// <summary>
        /// GAS出力フォルダからCSVをプロジェクトへコピーする（ダイアログなしの本体）。
        /// </summary>
        /// <returns>エラーメッセージ。正常時は null</returns>
        private static string CopyFilesCore(string selectedFolder)
        {
            var csvFiles = Directory.GetFiles(selectedFolder, WaveScalingCsvFileName, SearchOption.AllDirectories);
            if (csvFiles.Length == 0)
            {
                return $"以下のファイルが見つかりませんでした:\n・{WaveScalingCsvFileName}";
            }

            File.Copy(csvFiles[0], Path.GetFullPath(CsvPath), overwrite: true);
            return null;
        }

        [MenuItem("Tools/マスターデータ/WaveScalingData CSVインポート")]
        private static void ImportFromMenu() => ImportData(interactive: true);

        /// <summary>
        /// WaveScalingData.csv をインポートしてアセットと WaveScalingDatabase を更新する。
        /// </summary>
        /// <param name="interactive">
        /// true: 完了/エラーをダイアログ表示（メニュー実行向け）。
        /// false: ダイアログを出さずログに出力する（issue駆動などの自動実行向け）。
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

            // ヘッダー行（Row1）をスキップし、空行を除いたデータ行を収集
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

            // 先に全行を検証する。
            // 1行でも不正があるとアセット削除まで含めた反映が中途半端に効いてしまい、
            // 難易度カーブが黙って変わるため、全行が正しいときだけ書き込む
            var errors = new List<string>();
            var rows = new List<ParsedRow>();
            var seenWaves = new Dictionary<int, int>();
            var seenIds = new Dictionary<int, int>();

            for (var lineIdx = 0; lineIdx < dataLines.Count; lineIdx++)
            {
                var cols = dataLines[lineIdx].Split(',');
                var rowNum = lineIdx + 2; // ヘッダーを1行目とした場合の行番号

                if (cols.Length < MinColumnCount)
                {
                    errors.Add($"Row{rowNum}: 列数不足 (期待: {MinColumnCount}以上, 実際: {cols.Length})");
                    continue;
                }

                if (!TryParseInt(cols[ColId].Trim(), rowNum, "id", errors, out var id))
                {
                    continue;
                }

                // idはアセット名に使うため、重複すると同じアセットを上書きして片方が消える
                if (seenIds.TryGetValue(id, out var duplicatedIdRow))
                {
                    errors.Add($"Row{rowNum}: id {id} がRow{duplicatedIdRow}と重複しています");
                    continue;
                }

                if (!TryParseInt(cols[ColWave].Trim(), rowNum, "wave", errors, out var wave))
                {
                    continue;
                }

                // ウェーブ1が等倍の起点なので、それより小さい開始ウェーブは設定ミスとして弾く
                if (wave < 1)
                {
                    errors.Add($"Row{rowNum}: wave は1以上にしてください (実際: {wave})");
                    continue;
                }

                // 同じ開始ウェーブが複数あると、どちらが効くかが並び順任せになる
                if (seenWaves.TryGetValue(wave, out var duplicatedRow))
                {
                    errors.Add($"Row{rowNum}: wave {wave} がRow{duplicatedRow}と重複しています");
                    continue;
                }

                if (!TryParseFloat(cols[ColAtkBuff].Trim(), rowNum, "atkBuff", errors, out var atkBuff))
                {
                    continue;
                }

                if (!TryParseFloat(cols[ColHpBuff].Trim(), rowNum, "hpBuff", errors, out var hpBuff))
                {
                    continue;
                }

                // 負の増加率はウェーブを進むほど敵が弱くなるため、設定ミスとして弾く
                if (atkBuff < 0f || hpBuff < 0f)
                {
                    errors.Add($"Row{rowNum}: atkBuff / hpBuff は0以上にしてください (実際: {atkBuff} / {hpBuff})");
                    continue;
                }

                seenIds.Add(id, rowNum);
                seenWaves.Add(wave, rowNum);
                rows.Add(new ParsedRow { Id = id, Wave = wave, AtkBuff = atkBuff, HpBuff = hpBuff });
            }

            // 1行でも不正があれば何も書き換えずに中断する（部分適用させない）
            if (errors.Count > 0)
            {
                var errorMessage = $"CSVに不正な行があるためインポートを中断しました。\n\nエラー ({errors.Count} 件):\n"
                                   + string.Join("\n", errors);
                Notify(interactive, "インポート中断", errorMessage, isError: true);
                return false;
            }

            if (!AssetDatabase.IsValidFolder(OutputPath))
            {
                AssetDatabase.CreateFolder("Assets/App/MasterData", "WaveScaling");
            }

            var importedAssets = new List<WaveScalingMasterData>();
            var type = typeof(WaveScalingMasterData);

            foreach (var row in rows)
            {
                var assetPath = $"{OutputPath}/WaveScaling{row.Id}.asset";

                var masterData = AssetDatabase.LoadAssetAtPath<WaveScalingMasterData>(assetPath);
                var isNewAsset = masterData == null;
                if (isNewAsset)
                {
                    masterData = ScriptableObject.CreateInstance<WaveScalingMasterData>();
                }

                // ReadOnly の [SerializeField] へリフレクション経由で値をセット
                type.GetField("_id", PrivateInstance).SetValue(masterData, row.Id);
                type.GetField("_wave", PrivateInstance).SetValue(masterData, row.Wave);
                type.GetField("_atkBuff", PrivateInstance).SetValue(masterData, row.AtkBuff);
                type.GetField("_hpBuff", PrivateInstance).SetValue(masterData, row.HpBuff);

                // 値をセットしてからアセット化する。
                // 空のままCreateAssetすると、同じセッションで一度消したパスへ作り直したときに
                // 既定値で保存されたまま残ることがある
                if (isNewAsset)
                {
                    AssetDatabase.CreateAsset(masterData, assetPath);
                }

                EditorUtility.SetDirty(masterData);
                importedAssets.Add(masterData);
            }

            // 削除の前に確定させる（未保存の新規アセットが巻き添えで消えないように）
            AssetDatabase.SaveAssets();

            // CSVから消えた行のアセットを削除する。
            // 生成先フォルダはこのインポーターの専用なので、取り込めなかったものは残さない
            DeleteObsoleteAssets(importedAssets);

            // WaveScalingDatabase の配列を全件置き換え（無ければ新規作成）
            var database = AssetDatabase.LoadAssetAtPath<WaveScalingDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<WaveScalingDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var dbSo = new SerializedObject(database);
            var arrayProp = dbSo.FindProperty("_waveScalingMasterData");
            arrayProp.arraySize = importedAssets.Count;
            for (int i = 0; i < importedAssets.Count; i++)
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = importedAssets[i];
            dbSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Notify(interactive, "インポート完了", $"{importedAssets.Count} 件のアセットをインポートしました。", isError: false);
            return true;
        }

        /// <summary>
        /// 検証を通ったCSV1行ぶんの値。
        /// </summary>
        private struct ParsedRow
        {
            public int Id;
            public int Wave;
            public float AtkBuff;
            public float HpBuff;
        }

        /// <summary>
        /// 生成先フォルダにあるアセットのうち、今回のインポートに含まれないものを削除する。
        /// </summary>
        private static void DeleteObsoleteAssets(List<WaveScalingMasterData> importedAssets)
        {
            var importedPaths = new HashSet<string>();
            foreach (var asset in importedAssets)
            {
                importedPaths.Add(AssetDatabase.GetAssetPath(asset));
            }

            var guids = AssetDatabase.FindAssets($"t:{nameof(WaveScalingMasterData)}", new[] { OutputPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!importedPaths.Contains(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
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
                Debug.LogError($"[WaveScalingDataImporter] {title}: {message}");
            }
            else
            {
                Debug.Log($"[WaveScalingDataImporter] {title}: {message}");
            }
        }

        private static bool TryParseInt(string raw, int rowNum, string colName, List<string> errors, out int result)
        {
            if (int.TryParse(raw, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out result))
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
