using R3;

namespace App.Battle.DataStore
{
    public interface IPlayerDataStore
    {
        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }
        ReactiveProperty<float> Bullet { get; }
        ReactiveProperty<float> MaxBullet { get; }
        ReactiveProperty<bool> IsFocusLeft { get; }
        ReactiveProperty<bool> IsFocusRight { get; }
    }
}