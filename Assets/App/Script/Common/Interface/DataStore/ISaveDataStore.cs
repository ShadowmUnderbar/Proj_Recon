using App.Common.Data;

namespace App.Common.Interface
{
    public interface ISaveDataStore
    {
        SaveData SaveData { get; }
        void Save();
        SaveData Load();
        void ResetSaveData();
    }
}