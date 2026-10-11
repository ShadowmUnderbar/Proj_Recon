using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// エディタで再生したとき、開いているシーンにかかわらず起動シーン（Boot）から始める。
    /// Boot が共通の初期化を済ませてからメインメニューへ遷移するので、本番と同じ順序で動く。
    ///
    /// 再生開始シーンが既に別のシーンへ差し替えられている場合は触らない。
    /// デバッグ対戦（<see cref="DebugArenaLauncher"/>）やプレイテストのプローブは
    /// 再生開始シーンを Battle に差し替え、終わったら元に戻す（＝Boot に戻る）。
    ///
    /// シーン単体の見た目を確認したいときなどは、メニュー「Tools/Boot/Boot シーンから再生」で無効にできる。
    /// </summary>
    [InitializeOnLoad]
    public static class BootPlayModeStarter
    {
        public const string BootScenePath = "Assets/Scenes/Boot.unity";

        private const string MenuPath = "Tools/Boot/Boot シーンから再生";
        private const string EnabledKey = "BootPlayModeStarter.Enabled";

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

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 再生直前に毎回確認する。差し替えたツールが未設定（null）へ戻した場合もここで Boot に戻る
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Apply();
            }
        }

        private static void Apply()
        {
            if (EditorApplication.isPlaying)
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
