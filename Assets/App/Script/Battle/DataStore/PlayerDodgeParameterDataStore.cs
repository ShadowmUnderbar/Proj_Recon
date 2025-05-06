using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDodgeParameterDataStore : IPlayerDodgeParameterDataStore, IInitializable, ITickable
    {
        private float _dodgeCoolDown;
        public ReactiveProperty<float> DodgeCount { get; } = new();
        public ReactiveProperty<float> MaxDodgeCount { get; } = new(BasePlayerParameter.DodgeCount);
        public float DodgeDamage => BasePlayerParameter.DodgeDamage;

        public bool CanDodge => DodgeCount.Value > 0;

        public float DodgeRange => BasePlayerParameter.DodgeRange;


        public void Initialize()
        {
            DodgeCount.Value = MaxDodgeCount.Value;
        }

        public void SetCoolDownTime()
        {
            _dodgeCoolDown = BasePlayerParameter.DodgeCooldown;
            DodgeCount.Value--;
        }

        public void Tick()
        {
            if (DodgeCount.Value >= MaxDodgeCount.Value)
            {
                return;
            }

            _dodgeCoolDown -= Time.deltaTime;

            if (_dodgeCoolDown > 0)
            {
                return;
            }

            DodgeCount.Value++;
        }
    }
}