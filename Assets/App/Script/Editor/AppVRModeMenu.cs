using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    [InitializeOnLoad]
    public static class AppVRModeMenu
    {
        private const string MenuName = "App/VR Mode";
        private const string AllUnlockName = "App/AllUnlock";
        private const string VRModeKey = "VRMode";
        private const string AllUnLockKey = "AllUnLock";

        private static bool _isEnabled;
        private static bool _isAllUnLockEnabled;

        static AppVRModeMenu()
        {
            _isEnabled = EditorPrefs.GetBool("VRMode", false);
            _isAllUnLockEnabled = EditorPrefs.GetBool("AllUnLock", false);
        }

        [MenuItem(MenuName)]
        private static void ToggleAction()
        {
            _isEnabled = !_isEnabled;
            EditorPrefs.SetBool(VRModeKey, _isEnabled);
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
            EditorPrefs.SetBool(AllUnLockKey, _isAllUnLockEnabled);
        }

        [MenuItem(AllUnlockName, true)]
        private static bool ToggleAllUnlockNameActionValidate()
        {
            Menu.SetChecked(AllUnlockName, _isAllUnLockEnabled);
            return !Application.isPlaying;
        }
    }
}