using App.Common.Data;
using R3;

namespace App.Common.Interface
{
    public interface ISaveDataStore
    {
        Observable<Unit> OnLoad { get; }
        Observable<Unit> OnSave { get; }
        SaveData SaveData { get; }
        void Save();
        SaveData Load();
        void ResetSaveData();
    }
}