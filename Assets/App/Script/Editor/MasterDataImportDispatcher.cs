using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// Project ウィンドウで選択したマスターデータCSVのファイル名からインポーターを推測して実行する。
    /// 「UpgradeData.csv」なら「App.Editor.UpgradeDataImporter.ImportData(bool)」を呼ぶ規約。
    /// 新しいインポーターもこの命名と ImportData(bool) を守れば自動で対象になる
    /// </summary>
    public static class MasterDataImportDispatcher
    {
        private const string MenuPath = "Assets/マスターデータ/選択したCSVをインポート";
        private const string ToolsMenuPath = "Tools/マスターデータ/選択したCSVをインポート";
        private const string CsvExtension = ".csv";
        private const string ImporterNamespace = "App.Editor";
        private const string ImporterSuffix = "Importer";
        private const string ImportMethodName = "ImportData";

        [MenuItem(MenuPath)]
        [MenuItem(ToolsMenuPath)]
        private static void ImportSelected() => ImportCsvFiles(GetSelectedPaths(), interactive: true);

        [MenuItem(MenuPath, true)]
        [MenuItem(ToolsMenuPath, true)]
        private static bool ValidateImportSelected() => CollectTargets(GetSelectedPaths(), out _).Count > 0;

        /// <summary>
        /// 渡したCSVパスそれぞれのインポーターを推測して実行する。CSV以外のパスは無視する
        /// </summary>
        /// <param name="interactive">
        /// true: 各インポーターの完了/エラーをダイアログ表示（メニュー実行向け）。
        /// false: ダイアログを出さずログに出力する（自動実行向け）。
        /// </param>
        /// <returns>インポーターを推測でき、ImportData が true を返した件数</returns>
        public static int ImportCsvFiles(IEnumerable<string> assetPaths, bool interactive = false)
        {
            var targets = CollectTargets(assetPaths, out var unresolved);
            if (targets.Count == 0)
            {
                var message = "インポーターを推測できるCSVが選択されていません。\n" +
                              $"「<名前>.csv」に対して {ImporterNamespace}.<名前>{ImporterSuffix}.{ImportMethodName}(bool) を探します。" +
                              FormatUnresolved(unresolved);
                if (interactive)
                {
                    EditorUtility.DisplayDialog("エラー", message, "OK");
                }
                else
                {
                    Debug.LogError($"[MasterDataImportDispatcher] {message}");
                }

                return 0;
            }

            if (unresolved.Count > 0)
            {
                Debug.LogWarning("[MasterDataImportDispatcher] インポーターを推測できなかったファイルをスキップしました:" +
                                 FormatUnresolved(unresolved));
            }

            // 完了/エラーの通知は各インポーターが行うので、ここでは順に呼ぶだけ
            var succeeded = 0;
            foreach (var (csvPath, importMethod) in targets)
            {
                try
                {
                    if ((bool)importMethod.Invoke(null, new object[] { interactive }))
                    {
                        succeeded++;
                    }
                }
                catch (TargetInvocationException e)
                {
                    var inner = e.InnerException ?? e;
                    Debug.LogException(inner);
                    if (interactive)
                    {
                        EditorUtility.DisplayDialog("エラー", $"{csvPath} のインポート中に例外が発生しました:\n{inner.Message}", "OK");
                    }
                }
            }

            return succeeded;
        }

        private static IEnumerable<string> GetSelectedPaths()
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        /// <summary>
        /// パス群からCSVを拾い、対応するインポーターの ImportData を解決する。
        /// 同じインポーターは1回だけ実行する
        /// </summary>
        private static List<(string csvPath, MethodInfo importMethod)> CollectTargets(
            IEnumerable<string> assetPaths, out List<string> unresolved)
        {
            var targets = new List<(string, MethodInfo)>();
            var seen = new HashSet<MethodInfo>();
            unresolved = new List<string>();

            foreach (var path in assetPaths)
            {
                if (!string.Equals(Path.GetExtension(path), CsvExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var importMethod = FindImportMethod(Path.GetFileNameWithoutExtension(path));
                if (importMethod == null)
                {
                    unresolved.Add(path);
                    continue;
                }

                if (seen.Add(importMethod))
                {
                    targets.Add((path, importMethod));
                }
            }

            return targets;
        }

        /// <summary>
        /// ファイル名から「App.Editor.<ファイル名>Importer」の public static bool ImportData(bool) を探す
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

        private static string FormatUnresolved(List<string> unresolved)
        {
            return unresolved.Count == 0 ? string.Empty : "\n\n推測できなかったファイル:\n・" + string.Join("\n・", unresolved);
        }
    }
}
