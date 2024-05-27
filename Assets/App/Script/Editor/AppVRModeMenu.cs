using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    [InitializeOnLoad]
    public static class AppVRModeMenu
    {
        private const string MenuName = "App/VR Mode";
 
        private static bool _isEnabled;
 
        static AppVRModeMenu()
        {
            _isEnabled = EditorPrefs.GetBool("VRMode", false);
        }
 
        [MenuItem(MenuName)]
        private static void ToggleAction()
        {
            _isEnabled = !_isEnabled;
            EditorPrefs.SetBool("VRMode", _isEnabled);
        }
 
        [MenuItem(MenuName, true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(MenuName, _isEnabled);
            return !Application.isPlaying;
        }
    }
}