using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerDodgeParameterDataStore
    {
        public ReactiveProperty<float> DodgeCount { get; }
        public ReactiveProperty<float> MaxDodgeCount { get; }
        float DodgeDamage { get; }
        bool CanDodge { get; }
        float DodgeRange { get; }
        void SetCoolDownTime();
    }
}