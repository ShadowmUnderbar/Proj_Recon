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

        private static bool _isEnabled;
        private static bool _isAllUnLockEnabled;

        static AppVRModeMenu()
        {
            _isEnabled = EditorPrefs.GetBool(DebugConfig.VRModeKey, false);
            _isAllUnLockEnabled = EditorPrefs.GetBool(DebugConfig.AllUnLockKey, false);
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
    }
}