using System.Collections.Generic;

namespace App.Common.Interface
{
    /// <summary>
    /// ラン（1回のゲームプレイ）をまたいで持ち越すメタ進行データ。
    /// 獲得済みアップグレードのセットをスロット制で永続化する。
    /// </summary>
    public interface IMetaProgressionDataStore
    {
        /// <summary>保存スロット数</summary>
        int SlotCount { get; }

        /// <summary>指定スロットに保存されたアップグレードID一覧（未保存なら空）</summary>
        IReadOnlyList<string> GetSlotUpgradeIds(int slotIndex);

        /// <summary>指定スロットを保存したランの到達ウェーブ</summary>
        int GetSlotClearedWave(int slotIndex);

        /// <summary>指定スロットが未保存（空）か</summary>
        bool IsSlotEmpty(int slotIndex);

        /// <summary>
        /// 指定スロットにアップグレードセットを上書き保存し、セーブデータを永続化する。
        /// </summary>
        void SaveToSlot(int slotIndex, IReadOnlyList<string> upgradeIds, int clearedWave);
    }
}
