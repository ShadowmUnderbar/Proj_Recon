using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// マスターデータ取り込みの唯一の入口。エクスプローラーで選んだCSVのファイル名からインポーターを推測して実行する。
    /// 「UpgradeData.csv」なら Origin フォルダへコピーしたうえで「App.Editor.UpgradeDataImporter.ImportData(bool)」を呼ぶ規約。
    /// インポーターが EnumCsFiles を持っていれば、CSVと同じフォルダにある enum ファイルも一緒にコピーする。
    /// 新しいインポーターもこの命名と ImportData(bool) を守れば自動で対象になる
    /// </summary>
    public static class MasterDataImportDispatcher
    {
        private const string MenuName = "Tools/マスターデータ/インポート";
        private const string OriginFolder = "Assets/App/MasterData/Origin";
        private const string CsvExtension = "csv";
        private const string ImporterNamespace = "App.Editor";
        private const string ImporterSuffix = "Importer";
        private const string ImportMethodName = "ImportData";
        private const string EnumCsFilesFieldName = "EnumCsFiles";

        // 前回選んだフォルダを次回のダイアログの初期位置にする（個人ごとの利便用）
        private const string LastFolderPrefsKey = "App.MasterDataImportDispatcher.LastFolder";

        [MenuItem(MenuName)]
        private static void ImportFromMenu()
        {
            var lastFolder = EditorPrefs.GetString(LastFolderPrefsKey, string.Empty);
            var filePath = EditorUtility.OpenFilePanelWithFilters(
                "インポートするマスターデータを選択",
                lastFolder,
                new[] { "CSV", CsvExtension });
            if (string.IsNullOrEmpty(filePath)) return;

            EditorPrefs.SetString(LastFolderPrefsKey, Path.GetDirectoryName(filePath));
            ImportFile(filePath, interactive: true);
        }

        /// <summary>
        /// ファイル名からインポーターを推測し、CSV（と同じフォルダの enum ファイル）を取り込んでインポートする。
        /// すでに Origin 内のファイルならコピーしない。enum ファイルの内容が変わったときは再コンパイルが要るのでインポートせずに終える
        /// </summary>
        /// <param name="filePath">取り込むCSVのパス（プロジェクト外でもよい）</param>
        /// <param name="interactive">
        /// true: 完了/エラーをダイアログ表示（メニュー実行向け）。
        /// false: ダイアログを出さずログに出力する（自動実行向け）。
        /// </param>
        /// <returns>インポーターの ImportData が true を返したら true</returns>
        public static bool ImportFile(string filePath, bool interactive = false)
        {
            var fileName = Path.GetFileName(filePath);
            var importerType = FindImporterType(Path.GetFileNameWithoutExtension(filePath));
            var importMethod = FindImportMethod(importerType);
            if (importMethod == null)
            {
                Notify(interactive, "エラー",
                    $"「{fileName}」に対応するインポーターが見つかりません。\n" +
                    $"「<名前>.csv」に対して {ImporterNamespace}.<名前>{ImporterSuffix}.{ImportMethodName}(bool) を探します。",
                    isError: true);
                return false;
            }

            List<string> changedEnumFiles;
            try
            {
                changedEnumFiles = CopyIntoProject(filePath, importerType);
            }
            catch (Exception e)
            {
                Notify(interactive, "エラー", $"ファイルのコピーに失敗しました:\n{e.Message}", isError: true);
                return false;
            }

            if (changedEnumFiles.Count > 0)
            {
                // enum が変わると、再コンパイル前のインポートでは新しい値を解釈できず行が落ちる
                AssetDatabase.Refresh();
                Notify(interactive, "enumファイルを更新しました",
                    $"{string.Join(" / ", changedEnumFiles)} を更新しました。\nUnityの再コンパイル後に再度インポートを実行してください。",
                    isError: false);
                return false;
            }

            // 完了/エラーの通知はインポーターが行う
            try
            {
                return (bool)importMethod.Invoke(null, new object[] { interactive });
            }
            catch (TargetInvocationException e)
            {
                var inner = e.InnerException ?? e;
                Debug.LogException(inner);
                Notify(interactive, "エラー", $"{fileName} のインポート中に例外が発生しました:\n{inner.Message}", isError: true);
                return false;
            }
        }

        /// <summary>
        /// CSVを Origin へ、インポーターの EnumCsFiles に挙がった enum ファイルをCSVと同じフォルダからコピーする。
        /// enum ファイルは見つかったものだけコピーする（変更がなければGAS側で出力しなくてよい）
        /// </summary>
        /// <returns>内容が変わった enum ファイル名</returns>
        private static List<string> CopyIntoProject(string filePath, Type importerType)
        {
            var changedEnumFiles = new List<string>();
            var destPath = $"{OriginFolder}/{Path.GetFileName(filePath)}";
            var destAbsolute = Path.GetFullPath(destPath);
            if (string.Equals(Path.GetFullPath(filePath), destAbsolute, StringComparison.OrdinalIgnoreCase))
            {
                return changedEnumFiles;
            }

            File.Copy(filePath, destAbsolute, overwrite: true);
            AssetDatabase.ImportAsset(destPath);

            var sourceFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
            foreach (var (enumFileName, enumDestPath) in GetEnumCsFiles(importerType))
            {
                var enumSource = Path.Combine(sourceFolder, enumFileName);
                if (!File.Exists(enumSource)) continue;

                var enumDestAbsolute = Path.GetFullPath(enumDestPath);
                // GAS 出力は LF、git から取り出したファイルは CRLF になるため、改行コードの違いは同一とみなす。
                // 同一ならコピーもしない（改行コードだけの差分を作らない）
                if (File.Exists(enumDestAbsolute) &&
                    NormalizeLineEndings(File.ReadAllText(enumSource)) ==
                    NormalizeLineEndings(File.ReadAllText(enumDestAbsolute)))
                {
                    continue;
                }

                File.Copy(enumSource, enumDestAbsolute, overwrite: true);
                changedEnumFiles.Add(enumFileName);
            }

            return changedEnumFiles;
        }

        private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n");

        /// <summary>
        /// ファイル名から「App.Editor.&lt;ファイル名&gt;Importer」型を探す
        /// </summary>
        private static Type FindImporterType(string fileNameWithoutExtension)
        {
            var typeName = $"{ImporterNamespace}.{fileNameWithoutExtension}{ImporterSuffix}";
            return typeof(MasterDataImportDispatcher).Assembly.GetType(typeName);
        }

        /// <summary>
        /// インポーター型の public static bool ImportData(bool) を探す
        /// </summary>
        private static MethodInfo FindImportMethod(Type importerType)
        {
            var method = importerType?.GetMethod(
                ImportMethodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(bool) },
                null);
            return method != null && method.ReturnType == typeof(bool) ? method : null;
        }

        /// <summary>
        /// インポーター型の public static EnumCsFiles（ファイル名, コピー先パス）を読む。無ければ空
        /// </summary>
        private static (string fileName, string destPath)[] GetEnumCsFiles(Type importerType)
        {
            var field = importerType.GetField(EnumCsFilesFieldName, BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null) as (string fileName, string destPath)[]
                   ?? Array.Empty<(string fileName, string destPath)>();
        }

        /// <summary>
        /// 対話モードならダイアログ、自動実行モードならログで通知する。
        /// </summary>
        private static void Notify(bool interactive, string title, string message, bool isError)
        {
            if (interactive)
            {
                EditorUtility.DisplayDialog(title, message, "OK");
            }
            else if (isError)
            {
                Debug.LogError($"[MasterDataImportDispatcher] {title}: {message}");
            }
            else
            {
                Debug.Log($"[MasterDataImportDispatcher] {title}: {message}");
            }
        }
    }
}
