using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using VContainer;

namespace App.Battle.DataStore
{
    public class RunLevelDataStore : IRunLevelDataStore
    {
        private readonly ReactiveProperty<int> _runLevel = new(1);
        private readonly ReactiveProperty<float> _currentExperience = new(0f);
        private readonly Subject<Unit> _onLevelUp = new();
        private readonly RunLevelConfig _config;

        public ReadOnlyReactiveProperty<int> RunLevel => _runLevel;
        public ReadOnlyReactiveProperty<float> CurrentExperience => _currentExperience;
        public Observable<Unit> OnLevelUp => _onLevelUp;

        [Inject]
        public RunLevelDataStore(RunLevelConfig config)
        {
            _config = config;
        }

        public float GetRequiredExperience() =>
            _config.GetRequiredExperience(_runLevel.Value);

        public void AddExperience(float amount)
        {
            _currentExperience.Value += amount;
            while (_currentExperience.Value >= GetRequiredExperience())
            {
                _currentExperience.Value -= GetRequiredExperience();
                _runLevel.Value++;
                _onLevelUp.OnNext(Unit.Default);
            }
        }

        public void Reset()
        {
            _runLevel.Value = 1;
            _currentExperience.Value = 0f;
        }
    }
}
