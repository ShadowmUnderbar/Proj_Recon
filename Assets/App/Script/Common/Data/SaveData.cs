using System;
using System.Collections.Generic;

namespace App.Common.Data
{
    [Serializable]
    public class SaveData
    {
        public bool IsSwitchableFocus = false;
        public HandType DominantHand = HandType.Right;
        public int Exp = 0;
        public int Level = 1;
        public int Cash = 0;
        public PlayerUnlockType UnlockType { get; private set; } = PlayerUnlockType.None;

        // メタ進行: 保存済みアップグレードセット（スロット制）。
        // スロット数の正規化・アクセスは MetaProgressionDataStore が担う。
        public List<UpgradeSetSlot> UpgradeSetSlots = new();
    }
}
