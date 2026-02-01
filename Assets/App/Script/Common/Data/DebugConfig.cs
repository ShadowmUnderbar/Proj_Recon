#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Common.Data
{
    public static class DebugConfig
    {
#if !UNITY_EDITOR
        public static readonly bool IsVRMode = true;
#else
        public static readonly bool IsVRMode = EditorPrefs.GetBool("VRMode", false);
#endif

#if !UNITY_EDITOR
        public static readonly bool IsAllUnLock = false;
#else
        public static readonly bool IsAllUnLock = EditorPrefs.GetBool("AllUnLock", false);
#endif
    }
}