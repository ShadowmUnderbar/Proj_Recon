using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// エクスプローラーで選んだマスターデータCSVのファイル名からインポーターを推測して実行する。
    /// 「UpgradeData.csv」なら Origin フォルダへコピーしたうえで「App.Editor.UpgradeDataImporter.ImportData(bool)」を呼ぶ規約。
    /// 新しいインポーターもこの命名と ImportData(bool) を守れば自動で対象になる
    /// </summary>
    public static class MasterDataImportDispatcher
    {
        private const string MenuName = "App/マスターデータをインポート";
        private const string OriginFolder = "Assets/App/MasterData/Origin";
        private const string CsvExtension = "csv";
        private const string ImporterNamespace = "App.Editor";
        private const string ImporterSuffix = "Importer";
        private const string ImportMethodName = "ImportData";

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
        /// ファイル名からインポーターを推測し、CSVを Origin フォルダへコピーしてからインポートする。
        /// すでに Origin 内のファイルならコピーしない
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
            var importMethod = FindImportMethod(Path.GetFileNameWithoutExtension(filePath));
            if (importMethod == null)
            {
                Notify(interactive,
                    $"「{fileName}」に対応するインポーターが見つかりません。\n" +
                    $"「<名前>.csv」に対して {ImporterNamespace}.<名前>{ImporterSuffix}.{ImportMethodName}(bool) を探します。");
                return false;
            }

            var destPath = $"{OriginFolder}/{fileName}";
            try
            {
                var destAbsolute = Path.GetFullPath(destPath);
                if (!string.Equals(Path.GetFullPath(filePath), destAbsolute, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, destAbsolute, overwrite: true);
                    AssetDatabase.ImportAsset(destPath);
                }
            }
            catch (Exception e)
            {
                Notify(interactive, $"ファイルのコピーに失敗しました:\n{e.Message}");
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
                Notify(interactive, $"{fileName} のインポート中に例外が発生しました:\n{inner.Message}");
                return false;
            }
        }

        /// <summary>
        /// ファイル名から「App.Editor.&lt;ファイル名&gt;Importer」の public static bool ImportData(bool) を探す
        /// </summary>
        private static MethodInfo FindImportMethod(string fileNameWithoutExtension)
        {
            var typeName = $"{ImporterNamespace}.{fileNameWithoutExtension}{ImporterSuffix}";
            var type = typeof(MasterDataImportDispatcher).Assembly.GetType(typeName);
            var method = type?.GetMethod(
                ImportMethodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(bool) },
                null);
            return method != null && method.ReturnType == typeof(bool) ? method : null;
        }

        /// <summary>
        /// 対話モードならダイアログ、自動実行モードならエラーログで通知する。
        /// </summary>
        private static void Notify(bool interactive, string message)
        {
            if (interactive)
            {
                EditorUtility.DisplayDialog("エラー", message, "OK");
            }
            else
            {
                Debug.LogError($"[MasterDataImportDispatcher] {message}");
            }
        }
    }
}
