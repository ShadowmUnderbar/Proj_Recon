using App.Common.Data;
using App.Common.Interface;
using VContainer;

namespace App.Common.DataStore
{
    public class CoreSkillUnlockDataStore : ICoreSkillUnlockDataStore
    {
        private readonly ISaveDataStore _saveDataStore;

        [Inject]
        public CoreSkillUnlockDataStore(
            ISaveDataStore saveDataStore
        )
        {
            _saveDataStore = saveDataStore;
        }

        public bool IsUnLockAkimbo =>
            DebugConfig.IsAllUnLock ||
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Akimbo;

        public bool IsUnLockFocus =>
            DebugConfig.IsAllUnLock ||
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Focus;

        public bool IsUnLockWaltz =>
            DebugConfig.IsAllUnLock ||
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Waltz;

        public bool IsUnLockLongFocus =>
            DebugConfig.IsAllUnLock ||
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.LongFocus;

        public bool IsUnLockMerge =>
            DebugConfig.IsAllUnLock ||
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Merge;

        public bool IsUnLockBlitz =>
            DebugConfig.IsAllUnLock ||
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Blitz;
    }
}