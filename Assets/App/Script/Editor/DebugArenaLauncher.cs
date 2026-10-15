using App.Common.Data;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    /// <summary>
    /// デバッグ対戦の起動口。予約（<see cref="DebugArenaRequest"/>）を EditorPrefs に置き、Boot → Battle で再生する
    /// （起動シーンでの初期化を通す。再生開始シーンの差し替えと後片付けは <see cref="BootPlayModeStarter"/> が行う）。
    /// バトルのスコープが組み立て時に予約を1回だけ取り出して、通常のウェーブ進行の代わりに指定の相手を出す。
    /// 再生が終わったら、使われずに残った予約を消す（次の通常の再生へ持ち越さない）。
    /// ウィンドウ（<see cref="DebugArenaWindow"/>）のほか、プレイテストのプローブからも
    /// <see cref="Launch"/> または <see cref="Prepare"/>（再生は呼び出し側で始める）で使う。
    /// </summary>
    [InitializeOnLoad]
    public static class DebugArenaLauncher
    {
        static DebugArenaLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>予約して Boot → Battle で再生を始める。再生中は何もしない</summary>
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

        /// <summary>予約と Boot → Battle の準備だけを行う（再生は呼び出し側で始める）</summary>
        public static bool Prepare(DebugArenaRequest request)
        {
            if (request == null)
            {
                return false;
            }

            if (!BootPlayModeStarter.PrepareBattle())
            {
                return false;
            }

            EditorPrefs.SetString(DebugConfig.DebugArenaRequestKey, JsonUtility.ToJson(request));
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
        }
    }
}
