using System.Collections.Generic;
using App.Common.Data;
using App.Common.Interface;
using UnityEngine;
using VContainer;

namespace App.Common.DataStore
{
    /// <summary>
    /// アップグレードセットのスロット保存を管理する。
    /// 実体は <see cref="ISaveDataStore"/> の <see cref="SaveData.UpgradeSetSlots"/>。
    /// ラン終了（ゲームオーバー）時に、そのランで獲得したアップグレードをスロットへ保存する。
    /// </summary>
    public class MetaProgressionDataStore : IMetaProgressionDataStore
    {
        private const int Slots = 3;

        private readonly ISaveDataStore _saveDataStore;

        [Inject]
        public MetaProgressionDataStore(ISaveDataStore saveDataStore)
        {
            _saveDataStore = saveDataStore;
        }

        public int SlotCount => Slots;

        public IReadOnlyList<string> GetSlotUpgradeIds(int slotIndex)
        {
            if (!TryGetSlot(slotIndex, out var slot))
            {
                return System.Array.Empty<string>();
            }

            return slot.UpgradeIds;
        }

        public int GetSlotClearedWave(int slotIndex)
        {
            return TryGetSlot(slotIndex, out var slot) ? slot.ClearedWave : 0;
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            return !TryGetSlot(slotIndex, out var slot) || slot.IsEmpty;
        }

        public void SaveToSlot(int slotIndex, IReadOnlyList<string> upgradeIds, int clearedWave)
        {
            if (slotIndex < 0 || slotIndex >= Slots)
            {
                Debug.LogWarning($"[MetaProgressionDataStore] 不正なスロット番号: {slotIndex}");
                return;
            }

            EnsureSlots();

            var slot = _saveDataStore.SaveData.UpgradeSetSlots[slotIndex];
            slot.UpgradeIds = upgradeIds != null ? new List<string>(upgradeIds) : new List<string>();
            slot.ClearedWave = clearedWave;

            _saveDataStore.Save();
        }

        private bool TryGetSlot(int slotIndex, out UpgradeSetSlot slot)
        {
            EnsureSlots();

            if (slotIndex < 0 || slotIndex >= Slots)
            {
                slot = null;
                return false;
            }

            slot = _saveDataStore.SaveData.UpgradeSetSlots[slotIndex];
            return true;
        }

        /// <summary>
        /// セーブデータのスロット配列を必ず <see cref="Slots"/> 件に正規化する。
        /// 旧セーブデータや新規データでも常に固定スロット数を扱えるようにする。
        /// </summary>
        private void EnsureSlots()
        {
            var list = _saveDataStore.SaveData.UpgradeSetSlots;
            if (list == null)
            {
                list = new List<UpgradeSetSlot>();
                _saveDataStore.SaveData.UpgradeSetSlots = list;
            }

            while (list.Count < Slots)
            {
                list.Add(new UpgradeSetSlot());
            }
        }
    }
}
