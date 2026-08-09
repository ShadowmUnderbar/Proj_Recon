using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDodgeParameterDataStore : IPlayerDodgeParameterDataStore, IInitializable, ITickable, IDisposable
    {
        private float _dodgeCoolDown;

        private readonly Subject<Unit> _onDodge = new();
        public Observable<Unit> OnDodge => _onDodge;

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

            // 回避成立の通知（呼び出し元のPlayerDodgeUseCaseは可否判定を通過した後にのみ呼ぶ）
            _onDodge.OnNext(Unit.Default);
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

        public void Dispose()
        {
            _onDodge.Dispose();
        }
    }
}