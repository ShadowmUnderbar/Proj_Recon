using System;
using System.Collections.Generic;

namespace App.Common.Data
{
    /// <summary>
    /// 保存済みアップグレードセットの1スロット。
    /// ランで新たに獲得したアップグレードIDの集合と、そのランの到達ウェーブを保持する。
    /// JsonUtility でシリアライズするため [Serializable]＋publicフィールドで定義する。
    /// </summary>
    [Serializable]
    public class UpgradeSetSlot
    {
        // 保存されたアップグレードのID一覧（UpgradeMasterData.Id）
        public List<string> UpgradeIds = new();

        // このセットを保存したランの到達ウェーブ（表示用のメタ情報）
        public int ClearedWave = 0;

        // 中身が無い（未保存）スロットか
        public bool IsEmpty => UpgradeIds == null || UpgradeIds.Count == 0;
    }
}
