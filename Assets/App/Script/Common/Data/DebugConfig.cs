#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Common.Data
{
    public static class DebugConfig
    {
        public static string VRModeKey => "VRMode";
        public static string AllUnLockKey => "AllUnLock";

#if !UNITY_EDITOR
        public static readonly bool IsVRMode = true;
#else
        public static readonly bool IsVRMode = EditorPrefs.GetBool(VRModeKey, false);
#endif

#if !UNITY_EDITOR
        public static readonly bool IsAllUnLock = false;
#else
        public static readonly bool IsAllUnLock = EditorPrefs.GetBool(AllUnLockKey, false);
#endif
    }
}