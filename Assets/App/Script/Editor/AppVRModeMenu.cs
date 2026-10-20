using App.Common.Data;
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    [InitializeOnLoad]
    public static class AppVRModeMenu
    {
        private const string MenuName = "App/VR Mode";
        private const string AllUnlockName = "App/AllUnlock";
        private const string GazeTouchHitFeedbackName = "App/デバッグ: 注視判定に触れた敵をリアクションさせる";
        private const string VirusDebugLogName = "App/デバッグ: ウイルスのログを出す";

        private static bool _isEnabled;
        private static bool _isAllUnLockEnabled;
        private static bool _isGazeTouchHitFeedbackEnabled;
        private static bool _isVirusDebugLogEnabled;

        static AppVRModeMenu()
        {
            _isEnabled = EditorPrefs.GetBool(DebugConfig.VRModeKey, false);
            _isAllUnLockEnabled = EditorPrefs.GetBool(DebugConfig.AllUnLockKey, false);
            _isGazeTouchHitFeedbackEnabled = EditorPrefs.GetBool(DebugConfig.GazeTouchHitFeedbackKey, false);
            _isVirusDebugLogEnabled = EditorPrefs.GetBool(DebugConfig.VirusDebugLogKey, false);
        }

        [MenuItem(MenuName)]
        private static void ToggleAction()
        {
            _isEnabled = !_isEnabled;
            EditorPrefs.SetBool(DebugConfig.VRModeKey, _isEnabled);
        }

        [MenuItem(MenuName, true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(MenuName, _isEnabled);
            return !Application.isPlaying;
        }

        [MenuItem(AllUnlockName)]
        private static void ToggleAllUnlockNameAction()
        {
            _isAllUnLockEnabled = !_isAllUnLockEnabled;
            EditorPrefs.SetBool(DebugConfig.AllUnLockKey, _isAllUnLockEnabled);
        }

        [MenuItem(AllUnlockName, true)]
        private static bool ToggleAllUnlockNameActionValidate()
        {
            Menu.SetChecked(AllUnlockName, _isAllUnLockEnabled);
            return !Application.isPlaying;
        }

        // 視線が敵の判定球（EnemyGazeBoundsView）を通った瞬間に被弾リアクションを出す。判定球の大きさの確認用。
        // 設定はバトル開始時に読むため、再生中は切り替えられない
        [MenuItem(GazeTouchHitFeedbackName)]
        private static void ToggleGazeTouchHitFeedbackAction()
        {
            _isGazeTouchHitFeedbackEnabled = !_isGazeTouchHitFeedbackEnabled;
            EditorPrefs.SetBool(DebugConfig.GazeTouchHitFeedbackKey, _isGazeTouchHitFeedbackEnabled);
        }

        [MenuItem(GazeTouchHitFeedbackName, true)]
        private static bool ToggleGazeTouchHitFeedbackActionValidate()
        {
            Menu.SetChecked(GazeTouchHitFeedbackName, _isGazeTouchHitFeedbackEnabled);
            return !Application.isPlaying;
        }

        // ウイルスの発生・重複（効果時間・最長効果時間・威力）と1回ごとのダメージ量をログに出す（[Virus] で検索できる）。
        // 設定はバトル開始時に読むため、再生中は切り替えられない
        [MenuItem(VirusDebugLogName)]
        private static void ToggleVirusDebugLogAction()
        {
            _isVirusDebugLogEnabled = !_isVirusDebugLogEnabled;
            EditorPrefs.SetBool(DebugConfig.VirusDebugLogKey, _isVirusDebugLogEnabled);
        }

        [MenuItem(VirusDebugLogName, true)]
        private static bool ToggleVirusDebugLogActionValidate()
        {
            Menu.SetChecked(VirusDebugLogName, _isVirusDebugLogEnabled);
            return !Application.isPlaying;
        }
    }
}