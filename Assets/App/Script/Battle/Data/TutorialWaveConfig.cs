using System;
using System.Collections.Generic;
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ウェーブ開始時に出すチュートリアルの割り当て。
    /// 「何ウェーブ目の開始時に、どの種類のチュートリアル文言を出すか」を Inspector で指定する。
    /// 文言は Localization の TutorialText テーブルから「$」＋種類名のキーで引く（例: Wave1 → $Wave1）。
    /// 同じウェーブに複数割り当てた場合は先頭の1件だけ使う
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialWaveConfig", menuName = "Config/TutorialWaveConfig")]
    public class TutorialWaveConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("開始時に表示するウェーブ番号（1始まり）")]
            public int Wave = 1;

            [Tooltip("表示するチュートリアルの種類。TutorialText テーブルのキーは「$」＋この名前")]
            public TutorialType Type = TutorialType.Wave1;
        }

        [SerializeField, Tooltip("ウェーブ番号とチュートリアル種類の組")]
        private List<Entry> _entries = new();

        /// <summary>指定ウェーブの開始時に出すチュートリアルがあれば返す</summary>
        public bool TryGetTutorial(int wave, out TutorialType type)
        {
            foreach (var entry in _entries)
            {
                if (entry.Wave == wave)
                {
                    type = entry.Type;
                    return true;
                }
            }

            type = default;
            return false;
        }
    }
}
