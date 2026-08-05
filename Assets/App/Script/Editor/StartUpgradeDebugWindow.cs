using System.Collections.Generic;
using System.Linq;
using App.Common.Data;
using App.Common.Data.Database;
using App.Common.Data.MasterData;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// デバッグ用: 任意のアップグレードをチェックしておくと、次回のバトル開始時に所持済み状態で始められる。
    /// 選択結果は <see cref="DebugConfig.StartUpgradeIdsKey"/> のEditorPrefsに保存され、
    /// 実行時に <see cref="App.Battle.UseCase.RunStartUseCase"/> が読み取って付与する。
    /// </summary>
    public class StartUpgradeDebugWindow : EditorWindow
    {
        private const string MenuName = "App/デバッグ: 開始時アップグレード";

        private UpgradeDatabase _database;
        private readonly HashSet<string> _selectedIds = new();
        private string _searchText = string.Empty;
        private Vector2 _scrollPosition;

        [MenuItem(MenuName)]
        private static void Open()
        {
            var window = GetWindow<StartUpgradeDebugWindow>();
            window.titleContent = new GUIContent("開始時アップグレード");
            window.minSize = new Vector2(320f, 400f);
            window.Show();
        }

        private void OnEnable()
        {
            _database = LoadDatabase();
            LoadSelection();
        }

        private static UpgradeDatabase LoadDatabase()
        {
            var guid = AssetDatabase.FindAssets($"t:{nameof(UpgradeDatabase)}").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void LoadSelection()
        {
            _selectedIds.Clear();
            foreach (var id in DebugConfig.StartUpgradeIds)
            {
                _selectedIds.Add(id);
            }
        }

        private void SaveSelection()
        {
            // Database上の順序を保ってIDを並べる（保存内容の差分が安定するように）
            var ordered = _database == null
                ? _selectedIds.ToArray()
                : _database.UpgradeMasterData
                    .Where(data => data != null && _selectedIds.Contains(data.Id))
                    .Select(data => data.Id)
                    .ToArray();

            EditorPrefs.SetString(DebugConfig.StartUpgradeIdsKey, string.Join(",", ordered));
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("UpgradeDatabase が見つかりません。CSVインポートを実行してください。", MessageType.Error);
                if (GUILayout.Button("再読み込み"))
                {
                    _database = LoadDatabase();
                }

                return;
            }

            DrawHeader();
            DrawList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.HelpBox(
                "チェックしたアップグレードを所持した状態でバトルを開始します（Editor実行時のみ）。\n" +
                "同じアップグレードの複数レベルを選ぶと、ショップで重ね取りしたのと同じく効果が重複します。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"選択中: {_selectedIds.Count} 件", GUILayout.Width(100f));

                if (GUILayout.Button("全解除"))
                {
                    _selectedIds.Clear();
                    SaveSelection();
                }

                if (GUILayout.Button("表示中を全選択"))
                {
                    foreach (var data in GetFilteredUpgrades())
                    {
                        _selectedIds.Add(data.Id);
                    }

                    SaveSelection();
                }
            }

            _searchText = EditorGUILayout.TextField("検索", _searchText);

            // Database更新（CSV再インポート）で消えたIDが残っていないか知らせる
            var missing = _selectedIds
                .Where(id => !_database.TryGetUpgradeMasterData(id, out _))
                .ToArray();
            if (missing.Length > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Databaseに存在しないIDが選択されています: {string.Join(", ", missing)}",
                    MessageType.Warning);
                if (GUILayout.Button("存在しないIDを削除"))
                {
                    foreach (var id in missing)
                    {
                        _selectedIds.Remove(id);
                    }

                    SaveSelection();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawList()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition);
            _scrollPosition = scroll.scrollPosition;

            var lastType = (UpgradeType?)null;
            foreach (var data in GetFilteredUpgrades())
            {
                if (lastType != data.UpgradeType)
                {
                    lastType = data.UpgradeType;
                    EditorGUILayout.LabelField(data.UpgradeType.ToString(), EditorStyles.boldLabel);
                }

                var isSelected = _selectedIds.Contains(data.Id);
                var label = $"{TrimNameKey(data.NameKey)} Lv{data.Level}  (id:{data.Id})";

                var toggled = EditorGUILayout.ToggleLeft(label, isSelected);
                if (toggled == isSelected)
                {
                    continue;
                }

                if (toggled)
                {
                    _selectedIds.Add(data.Id);
                }
                else
                {
                    _selectedIds.Remove(data.Id);
                }

                SaveSelection();
            }
        }

        private IEnumerable<UpgradeMasterData> GetFilteredUpgrades()
        {
            var upgrades = _database.UpgradeMasterData
                .Where(data => data != null)
                .OrderBy(data => data.UpgradeType)
                .ThenBy(data => data.NameKey)
                .ThenBy(data => data.Level);

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return upgrades;
            }

            return upgrades.Where(data =>
                data.NameKey.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                data.UpgradeType.ToString().IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string TrimNameKey(string nameKey)
        {
            return string.IsNullOrEmpty(nameKey) ? "(名称なし)" : nameKey.TrimStart('$');
        }
    }
}
