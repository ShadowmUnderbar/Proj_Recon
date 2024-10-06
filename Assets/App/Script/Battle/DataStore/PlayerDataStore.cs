using R3;

namespace App.Battle.DataStore
{
    public class PlayerDataStore : IPlayerDataStore
    {
        public ReactiveProperty<float> Health { get; } = new();
        public ReactiveProperty<float> MaxHealth { get; } = new();
        public ReactiveProperty<float> Bullet { get; } = new();
        public ReactiveProperty<float> MaxBullet { get; } = new();
        public ReactiveProperty<int> IsFocusLeft { get; } = new();
        public ReactiveProperty<int> IsFocusRight { get; } = new();
    }
}