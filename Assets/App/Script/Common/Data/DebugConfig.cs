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

        /// <summary>デバッグ用「再生開始時にセーブデータを上書きする」設定の有効フラグ</summary>
        public static string SaveDataOverrideEnabledKey => "SaveDataOverrideEnabled";

        /// <summary>デバッグ用「再生開始時にセーブデータを上書きする」内容（SaveDataのJSON）</summary>
        public static string SaveDataOverrideJsonKey => "SaveDataOverrideJson";

        /// <summary>デバッグ用「視線が判定球を通った敵に被弾リアクションを出す」設定の有効フラグ</summary>
        public static string GazeTouchHitFeedbackKey => "GazeTouchHitFeedback";

        /// <summary>デバッグ用「敵と対戦」の予約内容（DebugArenaRequestのJSON）。次のバトルの組み立てで1回だけ使う</summary>
        public static string DebugArenaRequestKey => "DebugArenaRequest";

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
        public static bool IsGazeTouchHitFeedback => false;
#else
        // 実行のたびに読み直す（メニューでの変更をドメインリロード無しでも拾えるようにする）
        public static bool IsGazeTouchHitFeedback => EditorPrefs.GetBool(GazeTouchHitFeedbackKey, false);
#endif

#if !UNITY_EDITOR
        // 製品ビルドではセーブデータの上書きを行わない
        public static string SaveDataOverrideJson => null;
#else
        /// <summary>
        /// 再生開始時にセーブデータへ上書きするJSON。無効または未設定なら null。
        /// 実行のたびに読み直す（ウィンドウでの変更をドメインリロード無しでも拾えるようにする）
        /// </summary>
        public static string SaveDataOverrideJson =>
            EditorPrefs.GetBool(SaveDataOverrideEnabledKey, false)
                ? EditorPrefs.GetString(SaveDataOverrideJsonKey, null)
                : null;
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

#if !UNITY_EDITOR
        // 製品ビルドではデバッグ対戦を行わない
        public static string ConsumeDebugArenaRequestJson() => null;
#else
        /// <summary>
        /// デバッグ対戦の予約（DebugArenaRequestのJSON）を取り出して消す。予約が無ければ null。
        /// 1回で消すので、予約したバトルのリスタートは組み立て済みの設定で続き、
        /// メインメニューを経由した次のバトルは通常のランに戻る
        /// </summary>
        public static string ConsumeDebugArenaRequestJson()
        {
            var json = EditorPrefs.GetString(DebugArenaRequestKey, string.Empty);
            EditorPrefs.DeleteKey(DebugArenaRequestKey);
            return string.IsNullOrEmpty(json) ? null : json;
        }
#endif
    }
}
