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

        // 回避の直線移動用の状態
        private Vector3 _dodgeStartPosition;
        private Vector3 _dodgeTargetPosition;
        private float _dodgeElapsed;

        private readonly Subject<Unit> _onDodge = new();
        public Observable<Unit> OnDodge => _onDodge;

        private readonly Subject<float> _onDamagedDuringDodge = new();
        public Observable<float> OnDamagedDuringDodge => _onDamagedDuringDodge;

        private readonly ReactiveProperty<bool> _isDodging = new(false);
        public ReadOnlyReactiveProperty<bool> IsDodging => _isDodging;

        public ReactiveProperty<float> DodgeCount { get; } = new();
        public ReactiveProperty<float> MaxDodgeCount { get; } = new(BasePlayerParameter.DodgeCount);
        public float DodgeDamage => BasePlayerParameter.DodgeDamage;

        public bool CanDodge => DodgeCount.Value > 0;

        public float DodgeRange => BasePlayerParameter.DodgeRange;

        public float DodgeDuration => BasePlayerParameter.DodgeDuration;

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

        public void StartDodge(Vector3 start, Vector3 target)
        {
            _dodgeStartPosition = start;
            _dodgeTargetPosition = target;
            _dodgeElapsed = 0f;
            _isDodging.Value = true;
        }

        public bool TryAdvanceDodge(float deltaTime, out Vector3 position)
        {
            if (!_isDodging.Value)
            {
                position = _dodgeTargetPosition;
                return false;
            }

            _dodgeElapsed += deltaTime;

            // DodgeDurationが0以下なら従来どおり即座に到達させる
            if (DodgeDuration <= 0f || _dodgeElapsed >= DodgeDuration)
            {
                position = _dodgeTargetPosition;
                _isDodging.Value = false;
                return true;
            }

            position = Vector3.Lerp(_dodgeStartPosition, _dodgeTargetPosition, _dodgeElapsed / DodgeDuration);
            return true;
        }

        public void NotifyDamageBlocked(float damage)
        {
            if (!_isDodging.Value)
            {
                return;
            }

            _onDamagedDuringDodge.OnNext(damage);
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
            _onDamagedDuringDodge.Dispose();
            _isDodging.Dispose();
        }
    }
}
