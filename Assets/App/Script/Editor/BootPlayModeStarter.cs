using App.Common.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// エディタで再生したとき、開いているシーンにかかわらず起動シーン（Boot）から始める。
    /// Boot が共通の初期化を済ませてから遷移するので、本番と同じ順序で動く。
    ///
    /// 遷移先は2通り。
    /// - 通常の再生：Boot → MainMenu
    /// - 「Battle から再生」（<see cref="PlayBattle"/> / <see cref="PrepareBattle"/>）：Boot → Battle。
    ///   自動テスト（Tools/Playtest のランナー）やデバッグ対戦（<see cref="DebugArenaLauncher"/>）はこちらを使う
    ///
    /// 遷移先の予約は EditorPrefs（<see cref="DebugConfig.BootDestinationKey"/>）に置き、Boot が1回だけ取り出す。
    /// 再生が終わったら使われずに残った予約を消し、差し替えた再生開始シーンも元に戻す。
    ///
    /// シーン単体の見た目を確認したいときなどは、メニュー「Tools/Boot/Boot シーンから再生」で通常の再生だけ無効にできる。
    /// </summary>
    [InitializeOnLoad]
    public static class BootPlayModeStarter
    {
        public const string BootScenePath = "Assets/Scenes/Boot.unity";

        private const string ToggleMenuPath = "Tools/Boot/Boot シーンから再生";
        private const string PlayBattleMenuPath = "Tools/Boot/Battle から再生";
        private const string EnabledKey = "BootPlayModeStarter.Enabled";

        // ドメインリロードをまたいで「再生開始シーンを差し替えたか」と元のシーンを覚えておく
        private const string StartSceneOverriddenKey = "BootPlayModeStarter.StartSceneOverridden";
        private const string SavedStartScenePathKey = "BootPlayModeStarter.SavedStartScenePath";

        static BootPlayModeStarter()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            // エディタ起動直後・ドメインリロード後にも設定しておく（Play ボタンの表示上も Boot になる）
            EditorApplication.delayCall += Apply;
        }

        private static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        [MenuItem(ToggleMenuPath, priority = 0)]
        private static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Apply();
        }

        [MenuItem(ToggleMenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(ToggleMenuPath, IsEnabled);
            return true;
        }

        /// <summary>Boot で初期化してから Battle を始める（自動テスト・敵の実装確認用）</summary>
        [MenuItem(PlayBattleMenuPath, priority = 20)]
        public static void PlayBattle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[BootPlayModeStarter] 再生中は開始できません。再生を止めてから開始してください");
                return;
            }

            if (PrepareBattle())
            {
                EditorApplication.EnterPlaymode();
            }
        }

        /// <summary>
        /// 次の再生を Boot → Battle にする予約だけを行う（再生は呼び出し側で始める）。
        /// 「Boot シーンから再生」が無効でも、この再生だけは Boot から始める
        /// </summary>
        public static bool PrepareBattle()
        {
            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene == null)
            {
                Debug.LogError($"[BootPlayModeStarter] 起動シーンが見つかりません: {BootScenePath}");
                return false;
            }

            EditorPrefs.SetString(DebugConfig.BootDestinationKey, BootDestination.Battle.ToString());

            // 差し替え済みなら元のシーンは保存済みなので上書きしない（続けて2回呼ばれても元に戻せるように）
            if (!SessionState.GetBool(StartSceneOverriddenKey, false))
            {
                var saved = EditorSceneManager.playModeStartScene != null
                    ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
                    : string.Empty;
                SessionState.SetString(SavedStartScenePathKey, saved);
                SessionState.SetBool(StartSceneOverriddenKey, true);
            }

            EditorSceneManager.playModeStartScene = bootScene;
            return true;
        }

        /// <summary>
        /// <see cref="PrepareBattle"/> の予約を取り消し、再生開始シーンを元に戻す。
        /// 再生の終了時にも自動で呼ばれる。再生を始められなかったときの後片付けに使う
        /// </summary>
        public static void CancelPrepared()
        {
            EditorPrefs.DeleteKey(DebugConfig.BootDestinationKey);

            if (SessionState.GetBool(StartSceneOverriddenKey, false))
            {
                var savedPath = SessionState.GetString(SavedStartScenePathKey, string.Empty);
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(savedPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(savedPath);
                SessionState.EraseBool(StartSceneOverriddenKey);
                SessionState.EraseString(SavedStartScenePathKey);
            }

            Apply();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                // 再生直前に毎回確認する。差し替えたツールが未設定（null）へ戻した場合もここで Boot に戻る
                case PlayModeStateChange.ExitingEditMode:
                    Apply();
                    break;
                // 再生に失敗した（コンパイルエラー等）ときも、予約を次の通常の再生へ持ち越さない
                case PlayModeStateChange.EnteredEditMode:
                    CancelPrepared();
                    break;
            }
        }

        private static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            // 「Battle から再生」の予約中は差し替えた Boot をそのまま使う
            if (SessionState.GetBool(StartSceneOverriddenKey, false))
            {
                return;
            }

            var current = EditorSceneManager.playModeStartScene;
            var currentPath = current != null ? AssetDatabase.GetAssetPath(current) : string.Empty;

            if (!IsEnabled)
            {
                // 自分が設定した Boot だけを外す（他のツールの差し替えは残す）
                if (currentPath == BootScenePath)
                {
                    EditorSceneManager.playModeStartScene = null;
                }

                return;
            }

            if (current != null)
            {
                return;
            }

            var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene == null)
            {
                Debug.LogWarning($"[BootPlayModeStarter] 起動シーンが見つかりません: {BootScenePath}");
                return;
            }

            EditorSceneManager.playModeStartScene = bootScene;
        }
    }
}
