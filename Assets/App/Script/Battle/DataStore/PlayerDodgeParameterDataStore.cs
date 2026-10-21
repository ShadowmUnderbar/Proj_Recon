using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.DataStore
{
    public class PlayerDodgeParameterDataStore : IPlayerDodgeParameterDataStore, IRunResettable, IInitializable,
        ITickable, IDisposable
    {
        private readonly IFreezeDataStore _freezeDataStore;
        private readonly PlayerBaseParameterConfig _baseParameter;
        private readonly IHopStepsDataStore _hopStepsDataStore;

        private float _dodgeCoolDown;

        // 回避の直線移動用の状態
        private Vector3 _dodgeStartPosition;
        private Vector3 _dodgeTargetPosition;
        private float _dodgeElapsed;

        private readonly Subject<Unit> _onDodge = new();
        public Observable<Unit> OnDodge => _onDodge;

        private readonly Subject<PlayerDamagedData> _onDamagedDuringDodge = new();
        public Observable<PlayerDamagedData> OnDamagedDuringDodge => _onDamagedDuringDodge;

        private readonly Subject<DodgeEndData> _onDodgeEnd = new();
        public Observable<DodgeEndData> OnDodgeEnd => _onDodgeEnd;

        private readonly ReactiveProperty<bool> _isDodging = new(false);
        public ReadOnlyReactiveProperty<bool> IsDodging => _isDodging;

        public ReactiveProperty<float> DodgeCount { get; } = new();
        public ReactiveProperty<float> MaxDodgeCount { get; }
        public float DodgeDamage => _baseParameter.DodgeDamage;

        public bool CanDodge => DodgeCount.Value > 0;

        public float DodgeRange => _baseParameter.DodgeRange;

        public float DodgeDuration => _baseParameter.DodgeDuration;

        [Inject]
        public PlayerDodgeParameterDataStore(IFreezeDataStore freezeDataStore, PlayerBaseParameterConfig baseParameter,
            IHopStepsDataStore hopStepsDataStore)
        {
            _freezeDataStore = freezeDataStore;
            _baseParameter = baseParameter;
            _hopStepsDataStore = hopStepsDataStore;
            MaxDodgeCount = new ReactiveProperty<float>(baseParameter.DodgeCount);
        }

        public void Initialize()
        {
            DodgeCount.Value = MaxDodgeCount.Value;
        }

        public void ResetRun()
        {
            // 移動中にゲームオーバーになってもフラグが立ったままにならないよう、回避状態ごと畳む
            _isDodging.Value = false;
            _dodgeStartPosition = Vector3.zero;
            _dodgeTargetPosition = Vector3.zero;
            _dodgeElapsed = 0f;
            _dodgeCoolDown = 0f;

            // 強化の反映はセット読込・獲得時（UpgradeSideEffectApplier）にあらためて行うので、ここでは基礎値に戻すだけにする
            MaxDodgeCount.Value = _baseParameter.DodgeCount;
            DodgeCount.Value = MaxDodgeCount.Value;
        }

        public void RefreshMaxDodgeCount()
        {
            var newMaxDodgeCount = _baseParameter.DodgeCount + _hopStepsDataStore.GetExtraDodgeCount();
            var increased = newMaxDodgeCount - MaxDodgeCount.Value;

            MaxDodgeCount.Value = newMaxDodgeCount;

            // 上限が増えた分はその場で使えるようにする（取得した瞬間に回避回数が増える手応えを出す）。
            // 減った場合は上限でクランプするだけ
            DodgeCount.Value = increased > 0f
                ? Mathf.Min(newMaxDodgeCount, DodgeCount.Value + increased)
                : Mathf.Min(DodgeCount.Value, newMaxDodgeCount);
        }

        public void SetCoolDownTime()
        {
            _dodgeCoolDown = GetCoolDownTime();
            DodgeCount.Value--;

            // 回避成立の通知（呼び出し元のPlayerDodgeUseCaseは可否判定を通過した後にのみ呼ぶ）
            _onDodge.OnNext(Unit.Default);
        }

        public Vector3 DodgeTargetPosition => _dodgeTargetPosition;

        public Vector3 DodgeDirection
        {
            get
            {
                var direction = _dodgeTargetPosition - _dodgeStartPosition;
                direction.y = 0f;

                return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
            }
        }

        public float RemainingDodgeTime => Mathf.Max(0f, DodgeDuration - _dodgeElapsed);

        public void StartDodge(Vector3 start, Vector3 target)
        {
            _dodgeStartPosition = start;
            _dodgeTargetPosition = target;
            _dodgeElapsed = 0f;
            _isDodging.Value = true;
        }

        public bool TryAdvanceDodge(float deltaTime, out Vector3 position, out bool isFinished)
        {
            if (!_isDodging.Value)
            {
                position = _dodgeTargetPosition;
                isFinished = false;
                return false;
            }

            _dodgeElapsed += deltaTime;

            // DodgeDurationが0以下なら従来どおり即座に到達させる
            if (DodgeDuration <= 0f || _dodgeElapsed >= DodgeDuration)
            {
                position = _dodgeTargetPosition;
                _isDodging.Value = false;
                isFinished = true;
                return true;
            }

            position = Vector3.Lerp(_dodgeStartPosition, _dodgeTargetPosition, _dodgeElapsed / DodgeDuration);
            isFinished = false;
            return true;
        }

        public void NotifyDodgeEnd()
        {
            // 開始地点と終了地点が一致した場合は通知しない
            var direction = DodgeDirection;

            if (direction == Vector3.zero)
            {
                return;
            }

            _onDodgeEnd.OnNext(new DodgeEndData(_dodgeTargetPosition, direction));
        }

        public void NotifyDamageBlocked(PlayerDamagedData damagedData)
        {
            if (!_isDodging.Value)
            {
                return;
            }

            _onDamagedDuringDodge.OnNext(damagedData);
        }

        public void Tick()
        {
            // フリーズ中は回避そのものを止めるため、クールダウンの回復も止める
            if (_freezeDataStore.IsFreezing.CurrentValue)
            {
                return;
            }

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

            // 回避回数はクールダウンごとに1回ずつ戻す。まだ上限に届かなければ次の1回ぶんを待ち直す
            if (DodgeCount.Value < MaxDodgeCount.Value)
            {
                _dodgeCoolDown = GetCoolDownTime();
            }
        }

        // 回避1回ぶんが戻るまでの秒数（ホップステップの短縮を反映）
        private float GetCoolDownTime()
        {
            return _baseParameter.DodgeCooldown * _hopStepsDataStore.GetCooldownMultiplier();
        }

        public void Dispose()
        {
            _onDodge.Dispose();
            _onDamagedDuringDodge.Dispose();
            _onDodgeEnd.Dispose();
            _isDodging.Dispose();
        }
    }
}
