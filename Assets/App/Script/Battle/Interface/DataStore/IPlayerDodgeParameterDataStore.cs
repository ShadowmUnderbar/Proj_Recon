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

        /// <summary>回避が成立した瞬間に発火する（AfterDodge条件バフの起動に使う）</summary>
        Observable<Unit> OnDodge { get; }

        void SetCoolDownTime();
    }
}