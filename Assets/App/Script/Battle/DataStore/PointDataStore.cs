using App.Battle.Interface.DataStore;
using R3;

namespace App.Battle.DataStore
{
    public class PointDataStore : IPointDataStore
    {
        private readonly ReactiveProperty<int> _currentPoint = new(0);
        public ReadOnlyReactiveProperty<int> CurrentPoint => _currentPoint;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentPoint.Value += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (_currentPoint.Value < amount)
            {
                return false;
            }

            _currentPoint.Value -= amount;
            return true;
        }
    }
}
