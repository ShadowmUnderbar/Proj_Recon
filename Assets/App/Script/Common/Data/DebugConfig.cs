using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace App.Common.Data
{
    public static class DebugConfig
    {
        public static string VRModeKey => "VRMode";
        public static string AllUnLockKey => "AllUnLock";

        /// <summary>デバッグ用「最初から所持するアップグレード」のID一覧（カンマ区切りで保存）</summary>
        public static string StartUpgradeIdsKey => "StartUpgradeIds";

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

#if !UNITY_EDITOR
        // 製品ビルドではデバッグ付与を行わない
        public static IReadOnlyList<string> StartUpgradeIds => Array.Empty<string>();
#else
        // 実行のたびに読み直す（ウィンドウでの変更をドメインリロード無しでも拾えるようにする）
        public static IReadOnlyList<string> StartUpgradeIds =>
            EditorPrefs.GetString(StartUpgradeIdsKey, string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
#endif
    }
}
