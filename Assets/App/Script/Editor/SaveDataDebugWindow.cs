using System;
using System.Collections.Generic;
using System.IO;
using App.Common;
using App.Common.Data;
using App.Common.DataStore;
using App.Common.Interface;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace App.Editor
{
    /// <summary>
    /// デバッグ用: セーブデータを直接書き換えるウィンドウ。
    /// 「開始時アップグレード」（EditorPrefsに持つ実行時デバッグ設定）とは別枠で、
    /// こちらは SaveData.json そのものを書き換える。
    /// 操作は <see cref="Operations"/> に1件ずつ追加していく想定（今はスロット空化のみ）。
    /// 再生中は常駐スコープの <see cref="ISaveDataStore"/> に適用して即時反映し、停止中はファイルを直接読み書きする。
    /// </summary>
    public class SaveDataDebugWindow : EditorWindow
    {
        private const string MenuName = "App/デバッグ: セーブデータ";

        /// <summary>セーブデータへの1操作。ラベル・説明・SaveDataに対する変更内容の組</summary>
        private readonly struct Operation
        {
            public readonly string Label;
            public readonly string Description;
            public readonly Action<SaveData> Apply;

            public Operation(string label, string description, Action<SaveData> apply)
            {
                Label = label;
                Description = description;
                Apply = apply;
            }
        }

        private static readonly Operation[] Operations =
        {
            new(
                "アップグレードセットのスロットを全て空にする",
                "スロット3点の保存内容を消す。他のセーブ項目（設定・所持金など）は変えない",
                saveData => saveData.UpgradeSetSlots = new List<UpgradeSetSlot>()),
        };

        private SaveData _preview;
        private string _previewSource = string.Empty;

        // 再生中は SaveData の実体が差し替わる（ResetSaveData / 再Load）ことがあるので、
        // 参照を持ち続けず毎回ストアから取り直す
        private ISaveDataStore _runtimeSaveDataStore;

        [MenuItem(MenuName)]
        private static void Open()
        {
            var window = GetWindow<SaveDataDebugWindow>();
            window.titleContent = new GUIContent("セーブデータ");
            window.minSize = new Vector2(360f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshPreview();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>再生開始・停止でデータの取得元が変わるので、プレビューを取り直す</summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                RefreshPreview();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "セーブデータ（SaveData.json）を直接書き換えます。\n" +
                "再生中は動作中のSaveDataStoreに適用して保存、停止中はファイルを直接更新します。",
                MessageType.Info);

            DrawPreview();
            EditorGUILayout.Space();
            DrawOperations();
        }

        private void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"現在の内容（{_previewSource}）", EditorStyles.boldLabel);
                if (GUILayout.Button("再読み込み", GUILayout.Width(90f)))
                {
                    RefreshPreview();
                }
            }

            var preview = _runtimeSaveDataStore != null ? _runtimeSaveDataStore.SaveData : _preview;
            if (preview == null)
            {
                EditorGUILayout.HelpBox("セーブデータを読み込めませんでした。", MessageType.Warning);
                return;
            }

            var slots = preview.UpgradeSetSlots;
            if (slots == null || slots.Count == 0)
            {
                EditorGUILayout.LabelField("アップグレードセット: スロット無し（全て空）");
                return;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var text = slot == null || slot.IsEmpty
                    ? "(空)"
                    : $"{slot.UpgradeIds.Count}個 / Wave{slot.ClearedWave}";
                EditorGUILayout.LabelField($"スロット{i + 1}: {text}");
            }
        }

        private void DrawOperations()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            foreach (var operation in Operations)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(operation.Description, EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button(operation.Label))
                    {
                        Execute(operation);
                    }
                }
            }
        }

        private void Execute(Operation operation)
        {
            var confirmed = EditorUtility.DisplayDialog(
                "セーブデータの書き換え",
                $"「{operation.Label}」を実行します。元に戻せません。よろしいですか？",
                "実行",
                "キャンセル");
            if (!confirmed)
            {
                return;
            }

            try
            {
                var saveDataStore = FindRuntimeSaveDataStore();
                if (saveDataStore != null)
                {
                    // 再生中: 動作中のインスタンスへ適用し、その場で保存する
                    operation.Apply(saveDataStore.SaveData);
                    saveDataStore.Save();
                    Debug.Log($"[SaveDataDebugWindow] 再生中のセーブデータに適用しました: {operation.Label}");
                }
                else
                {
                    // 停止中（または常駐スコープが無いシーン）: ファイルを読み込んで書き換え、保存する。
                    // SaveDataStore.Load は解析失敗を握りつぶして初期値を返すため、
                    // 壊れたファイルを初期値で上書きしないよう先に解析できるか確かめる
                    if (!CanParseSaveFile(out var reason))
                    {
                        Debug.LogError($"[SaveDataDebugWindow] セーブファイルを解析できないため書き換えを中止しました: {reason}");
                        return;
                    }

                    using var fileStore = new SaveDataStore();
                    fileStore.Load();
                    operation.Apply(fileStore.SaveData);
                    fileStore.Save();
                    Debug.Log($"[SaveDataDebugWindow] セーブファイルを書き換えました: {operation.Label}");

                    if (EditorApplication.isPlaying)
                    {
                        Debug.LogWarning("[SaveDataDebugWindow] 再生中ですが CommonLifetimeScope が見つからないため、" +
                                         "ファイルのみ更新しました。動作中のデータには反映されていません");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataDebugWindow] セーブデータの書き換えに失敗しました: {e.Message}");
            }

            RefreshPreview();
        }

        private void RefreshPreview()
        {
            try
            {
                _runtimeSaveDataStore = FindRuntimeSaveDataStore();
                if (_runtimeSaveDataStore != null)
                {
                    _preview = null;
                    _previewSource = "再生中のデータ";
                    return;
                }

                using var fileStore = new SaveDataStore();
                _preview = fileStore.Load();
                _previewSource = "ファイル";
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataDebugWindow] セーブデータの読み込みに失敗しました: {e.Message}");
                _runtimeSaveDataStore = null;
                _preview = null;
                _previewSource = "読み込み失敗";
            }
        }

        /// <summary>
        /// セーブファイルが無い（新規扱い）か、JSONとして解析できるかを確かめる。
        /// 解析できないファイルを初期値で上書きしてしまわないための事前確認
        /// </summary>
        private static bool CanParseSaveFile(out string reason)
        {
            var path = SaveDataStore.SaveDataPath;
            if (!File.Exists(path))
            {
                reason = string.Empty;
                return true;
            }

            try
            {
                string json;
                using (var reader = new StreamReader(path))
                {
                    json = reader.ReadToEnd();
                }

                if (JsonUtility.FromJson<SaveData>(json) == null)
                {
                    reason = "内容が空です";
                    return false;
                }

                reason = string.Empty;
                return true;
            }
            catch (Exception e)
            {
                reason = e.Message;
                return false;
            }
        }

        /// <summary>再生中なら常駐スコープから動作中の <see cref="ISaveDataStore"/> を取り出す。見つからなければ null</summary>
        private static ISaveDataStore FindRuntimeSaveDataStore()
        {
            if (!EditorApplication.isPlaying)
            {
                return null;
            }

            var scope = FindObjectOfType<CommonLifetimeScope>();
            if (scope == null || scope.Container == null)
            {
                return null;
            }

            return scope.Container.Resolve<ISaveDataStore>();
        }
    }
}
