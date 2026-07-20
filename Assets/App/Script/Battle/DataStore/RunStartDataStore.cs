using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    public class RunStartDataStore : IRunStartDataStore
    {
        private readonly ReactiveProperty<bool> _isSelecting = new(false);
        public ReadOnlyReactiveProperty<bool> IsSelecting => _isSelecting;

        public void SetSelecting(bool isSelecting)
        {
            _isSelecting.Value = isSelecting;
        }
    }
}
