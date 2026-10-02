using App.Common.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// デバッグ対戦の起動口。予約（<see cref="DebugArenaRequest"/>）を EditorPrefs に置き、再生開始シーンを Battle にして再生する。
    /// バトルのスコープが組み立て時に予約を1回だけ取り出して、通常のウェーブ進行の代わりに指定の相手を出す。
    /// 再生が終わったら、再生開始シーンを元に戻し、使われずに残った予約も消す（次の通常の再生へ持ち越さない）。
    /// ウィンドウ（<see cref="DebugArenaWindow"/>）のほか、プレイテストのプローブからも
    /// <see cref="Launch"/> または <see cref="Prepare"/>（再生は呼び出し側で始める）で使う。
    /// </summary>
    [InitializeOnLoad]
    public static class DebugArenaLauncher
    {
        public const string BattleScenePath = "Assets/Scenes/Battle.unity";

        // ドメインリロードをまたいで「再生開始シーンを差し替えたか」と元のシーンを覚えておく
        private const string StartSceneOverriddenKey = "DebugArenaLauncher.StartSceneOverridden";
        private const string SavedStartScenePathKey = "DebugArenaLauncher.SavedStartScenePath";

        static DebugArenaLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>予約して Battle シーンで再生を始める。再生中は何もしない</summary>
        public static bool Launch(DebugArenaRequest request)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[DebugArenaLauncher] 再生中は開始できません。再生を止めてから開始してください");
                return false;
            }

            if (!Prepare(request))
            {
                return false;
            }

            EditorApplication.EnterPlaymode();
            return true;
        }

        /// <summary>予約と再生開始シーンの差し替えだけを行う（再生は呼び出し側で始める）</summary>
        public static bool Prepare(DebugArenaRequest request)
        {
            if (request == null)
            {
                return false;
            }

            var battleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath);
            if (battleScene == null)
            {
                Debug.LogError($"[DebugArenaLauncher] Battle シーンが見つかりません: {BattleScenePath}");
                return false;
            }

            EditorPrefs.SetString(DebugConfig.DebugArenaRequestKey, JsonUtility.ToJson(request));

            // 差し替え済みなら元のシーンは保存済みなので上書きしない（続けて2回呼ばれても元に戻せるように）
            if (!SessionState.GetBool(StartSceneOverriddenKey, false))
            {
                var saved = EditorSceneManager.playModeStartScene != null
                    ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
                    : string.Empty;
                SessionState.SetString(SavedStartScenePathKey, saved);
                SessionState.SetBool(StartSceneOverriddenKey, true);
            }

            EditorSceneManager.playModeStartScene = battleScene;
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            // 再生に失敗した（コンパイルエラー等）ときも、予約を次の通常の再生へ持ち越さない
            EditorPrefs.DeleteKey(DebugConfig.DebugArenaRequestKey);

            if (!SessionState.GetBool(StartSceneOverriddenKey, false))
            {
                return;
            }

            var savedPath = SessionState.GetString(SavedStartScenePathKey, string.Empty);
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(savedPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(savedPath);
            SessionState.EraseBool(StartSceneOverriddenKey);
            SessionState.EraseString(SavedStartScenePathKey);
        }
    }
}
