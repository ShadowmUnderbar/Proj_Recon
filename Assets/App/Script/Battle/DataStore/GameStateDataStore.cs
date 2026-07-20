using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    public class GameStateDataStore : IGameStateDataStore
    {
        private readonly ReactiveProperty<bool> _isGameOver = new(false);
        public ReadOnlyReactiveProperty<bool> IsGameOver => _isGameOver;

        public void SetGameOver()
        {
            if (_isGameOver.Value)
            {
                return;
            }

            _isGameOver.Value = true;
        }
    }
}
