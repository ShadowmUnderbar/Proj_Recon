using App.Common.Data.MasterData;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IRunLevelDataStore
    {
        ReadOnlyReactiveProperty<int> RunLevel { get; }
        ReadOnlyReactiveProperty<float> CurrentExperience { get; }
        Observable<Unit> OnLevelUp { get; }

        float GetRequiredExperience();
        void AddExperience(float amount);
        void Reset();
    }
}
