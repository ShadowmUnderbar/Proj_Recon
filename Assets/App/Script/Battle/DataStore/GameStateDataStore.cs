using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    public class GameStateDataStore : IGameStateDataStore, IRunResettable
    {
        private readonly ReactiveProperty<bool> _isGameOver = new(false);
        public ReadOnlyReactiveProperty<bool> IsGameOver => _isGameOver;

        private readonly ReactiveProperty<bool> _isCleared = new(false);
        public ReadOnlyReactiveProperty<bool> IsCleared => _isCleared;

        public bool IsRunEnded => _isGameOver.Value || _isCleared.Value;

        public void ResetRun()
        {
            _isGameOver.Value = false;
            _isCleared.Value = false;
        }

        public void SetGameOver()
        {
            if (IsRunEnded)
            {
                return;
            }

            _isGameOver.Value = true;
        }

        public void SetCleared()
        {
            if (IsRunEnded)
            {
                return;
            }

            _isCleared.Value = true;
        }
    }
}
