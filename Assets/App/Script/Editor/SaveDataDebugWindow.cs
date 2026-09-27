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
    /// デバッグ用: セーブデータを編集して上書きするウィンドウ。
    /// 「開始時アップグレード」（EditorPrefsに持つ実行時デバッグ設定）とは別枠。
    /// 編集欄の内容は EditorPrefs に保存され、「再生開始時に上書き」を有効にしておくと
    /// 再生開始時のセーブデータ読み込み直後に <see cref="SaveDataStore"/> が読み取って上書きする（予約上書き）。
    /// 再生中は「今すぐ上書き」で動作中の <see cref="ISaveDataStore"/> へ直接書き戻して保存もできる。
    /// SaveData の public フィールドを全て編集できるので、今後の項目追加でも手を入れずに使える
    /// （JsonUtility の対象外である private setter のプロパティ（UnlockType 等）は出ない）。
    /// よく使う編集はショートカット（今はスロット3点の空化）としてボタンを用意する。
    /// </summary>
    public class SaveDataDebugWindow : EditorWindow
    {
        private const string MenuName = "App/デバッグ: セーブデータ";

        /// <summary>SerializedObject で編集するための入れ物。SaveData は plain class なので ScriptableObject に包む</summary>
        private class SaveDataHolder : ScriptableObject
        {
            public SaveData Data = new();
        }

        private SaveDataHolder _holder;
        private SerializedObject _serializedHolder;
        private ISaveDataStore _runtimeSaveDataStore;
        private Vector2 _scrollPosition;

        // 再生開始直後にスコープが未構築だった場合に備え、再生中は一定間隔でストアを取り直す
        private const double RuntimeStoreRetryIntervalSeconds = 1.0;
        private double _nextRuntimeStoreRetryTime;

        [MenuItem(MenuName)]
        private static void Open()
        {
            var window = GetWindow<SaveDataDebugWindow>();
            window.titleContent = new GUIContent("セーブデータ");
            window.minSize = new Vector2(380f, 440f);
            window.Show();
        }

        private void OnEnable()
        {
            _holder = CreateInstance<SaveDataHolder>();
            _holder.hideFlags = HideFlags.HideAndDontSave;
            _serializedHolder = new SerializedObject(_holder);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // 編集欄は EditorPrefs の保存内容を優先し、無ければ現在のセーブファイルから起こす
            if (!LoadFromPrefs())
            {
                LoadFromFile();
            }

            _runtimeSaveDataStore = FindRuntimeSaveDataStore();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            _serializedHolder?.Dispose();
            if (_holder != null)
            {
                DestroyImmediate(_holder);
            }
        }

        /// <summary>再生開始でストアを掴み、停止で手放す。編集欄は EditorPrefs 側が正なので触らない</summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _runtimeSaveDataStore = FindRuntimeSaveDataStore();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _runtimeSaveDataStore = null;
            }

            Repaint();
        }

        private void OnGUI()
        {
            RetryBindRuntimeStore();

            EditorGUILayout.HelpBox(
                "編集欄の内容でセーブデータを上書きします（Editor実行時のみ）。\n" +
                "・再生開始時に上書き: 有効にしておくと、再生開始時のセーブデータ読み込み直後に編集欄の内容で上書きします\n" +
                "・今すぐ上書き: 再生中に、動作中のセーブデータへ書き戻して保存します",
                MessageType.Info);

            DrawStartupOverride();
            EditorGUILayout.Space();
            DrawSourceButtons();
            EditorGUILayout.Space();
            DrawEditor();
            EditorGUILayout.Space();
            DrawShortcuts();
        }

        /// <summary>再生中なのにストアを掴めていなければ、間隔を空けて取り直す（毎フレーム FindObjectOfType しない）</summary>
        private void RetryBindRuntimeStore()
        {
            if (!EditorApplication.isPlaying || _runtimeSaveDataStore != null)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextRuntimeStoreRetryTime)
            {
                return;
            }

            _nextRuntimeStoreRetryTime = EditorApplication.timeSinceStartup + RuntimeStoreRetryIntervalSeconds;
            _runtimeSaveDataStore = FindRuntimeSaveDataStore();
        }

        private void DrawStartupOverride()
        {
            var enabled = EditorPrefs.GetBool(DebugConfig.SaveDataOverrideEnabledKey, false);
            var toggled = EditorGUILayout.ToggleLeft("再生開始時に編集欄の内容で上書きする", enabled);
            if (toggled != enabled)
            {
                EditorPrefs.SetBool(DebugConfig.SaveDataOverrideEnabledKey, toggled);
            }

            if (toggled)
            {
                EditorGUILayout.HelpBox(
                    "有効です。ファイルは書き換えず、再生ごとにメモリ上のセーブデータへ上書きします（ゲーム側が保存すれば残ります）。",
                    MessageType.Warning);
            }
        }

        private void DrawSourceButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("セーブファイルから読み込み"))
                {
                    LoadFromFile();
                }

                using (new EditorGUI.DisabledScope(_runtimeSaveDataStore == null))
                {
                    if (GUILayout.Button("再生中のデータから読み込み"))
                    {
                        LoadFromRuntime();
                    }

                    if (GUILayout.Button("今すぐ上書き"))
                    {
                        OverwriteRuntime();
                    }
                }
            }

            if (_runtimeSaveDataStore == null)
            {
                EditorGUILayout.LabelField(
                    EditorApplication.isPlaying
                        ? "再生中ですが CommonLifetimeScope が見つかりません（常駐スコープのあるシーンで再生してください）"
                        : "「再生中のデータから読み込み」「今すぐ上書き」は再生中のみ使えます",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawEditor()
        {
            EditorGUILayout.LabelField("編集欄（変更は自動で保存されます）", EditorStyles.boldLabel);

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition);
            _scrollPosition = scroll.scrollPosition;

            _serializedHolder.Update();
            var dataProperty = _serializedHolder.FindProperty(nameof(SaveDataHolder.Data));

            // SaveData の直下フィールドを順に描く（フィールド追加時にウィンドウ側の変更が要らないように）
            var property = dataProperty.Copy();
            var end = property.GetEndProperty();
            var enterChildren = true;
            while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, end))
            {
                EditorGUILayout.PropertyField(property, true);
                enterChildren = false;
            }

            if (_serializedHolder.ApplyModifiedPropertiesWithoutUndo())
            {
                SaveToPrefs();
            }
        }

        private void DrawShortcuts()
        {
            EditorGUILayout.LabelField("編集ショートカット（編集欄を書き換えるだけ）", EditorStyles.boldLabel);

            if (GUILayout.Button("アップグレードセットのスロットを全て空にする"))
            {
                _holder.Data.UpgradeSetSlots = new List<UpgradeSetSlot>();
                // 直接フィールドを書き換えたので SerializedObject 側を取り直す
                _serializedHolder.Update();
                SaveToPrefs();
            }
        }

        /// <summary>編集欄の内容を EditorPrefs に保存する。再生開始時の上書きはこれを読む</summary>
        private void SaveToPrefs()
        {
            EditorPrefs.SetString(DebugConfig.SaveDataOverrideJsonKey, JsonUtility.ToJson(_holder.Data));
        }

        /// <summary>EditorPrefs に保存済みの編集内容を編集欄へ戻す。保存が無ければ false</summary>
        private bool LoadFromPrefs()
        {
            var json = EditorPrefs.GetString(DebugConfig.SaveDataOverrideJsonKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, _holder.Data);
                _serializedHolder.Update();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataDebugWindow] 保存済みの編集内容の読み込みに失敗しました: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 現在のセーブファイルの内容を編集欄へ写す（ファイルは書き換えない）。
        /// SaveDataStore.Load は予約上書きを適用してしまうので通さず、ファイルを直接読む。無ければ初期値
        /// </summary>
        private void LoadFromFile()
        {
            try
            {
                var path = SaveDataStore.SaveDataPath;
                if (!File.Exists(path))
                {
                    CopyInto(new SaveData());
                    return;
                }

                string json;
                using (var reader = new StreamReader(path))
                {
                    json = reader.ReadToEnd();
                }

                CopyInto(JsonUtility.FromJson<SaveData>(json) ?? new SaveData());
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataDebugWindow] セーブファイルの読み込みに失敗しました: {e.Message}");
            }
        }

        /// <summary>再生中のセーブデータを編集欄へ写す（参照を共有しない）</summary>
        private void LoadFromRuntime()
        {
            try
            {
                CopyInto(_runtimeSaveDataStore.SaveData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataDebugWindow] 再生中のセーブデータの読み込みに失敗しました: {e.Message}");
            }
        }

        private void CopyInto(SaveData source)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), _holder.Data);
            _serializedHolder.Update();
            SaveToPrefs();
        }

        /// <summary>再生中のセーブデータを編集欄の内容で上書きして保存する（確認ダイアログ付き）</summary>
        private void OverwriteRuntime()
        {
            var confirmed = EditorUtility.DisplayDialog(
                "セーブデータの上書き",
                "編集欄の内容で再生中のセーブデータを上書きし、ファイルにも保存します。よろしいですか？",
                "上書き",
                "キャンセル");
            if (!confirmed)
            {
                return;
            }

            ApplyToRuntime();
        }

        /// <summary>
        /// 確認ダイアログを挟まない上書きの実体。編集欄の内容を動作中の SaveData へ書き戻して保存する。
        /// インスタンスを差し替えず FromJsonOverwrite で中身だけ上書きするので、
        /// SaveData を参照している各DataStoreはそのまま新しい値を見る。
        /// </summary>
        private void ApplyToRuntime()
        {
            try
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_holder.Data), _runtimeSaveDataStore.SaveData);
                _runtimeSaveDataStore.Save();
                Debug.Log("[SaveDataDebugWindow] 再生中のセーブデータを上書きしました");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataDebugWindow] セーブデータの上書きに失敗しました: {e.Message}");
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

            try
            {
                return scope.Container.Resolve<ISaveDataStore>();
            }
            catch (Exception e)
            {
                // 再生終了直前などコンテナが破棄済みのタイミングで呼ばれることがある
                Debug.LogWarning($"[SaveDataDebugWindow] ISaveDataStore を解決できませんでした: {e.Message}");
                return null;
            }
        }
    }
}
