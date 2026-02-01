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
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Akimbo;

        public bool IsUnLockFocus =>
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Focus;

        public bool IsUnLockWaltz =>
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Waltz;

        public bool IsUnLockLongFocus =>
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.LongFocus;

        public bool IsUnLockMerge =>
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Merge;

        public bool IsUnLockCatalyst =>
            _saveDataStore.SaveData.UnlockType >= PlayerUnlockType.Catalyst;
    }
}