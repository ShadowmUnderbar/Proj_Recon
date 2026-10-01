using App.Battle.Data;
using App.Battle.Interface.EnemyAI;
using R3;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI.Boss
{
    /// <summary>
    /// ボスグループの台本から命令を受けて動くボスAIの基底。
    /// 自分では攻撃を抽選せず、命令された行動を 予備動作 → 攻撃 → 硬直 の順に進める。
    /// 各段階の秒数は EnemyMasterData の WindupTime / ActiveTime / RecoveryTime。
    /// 派生クラスは <see cref="OnActionActive"/>（攻撃の中身）と <see cref="BattleMove"/>（移動）を実装する。
    /// </summary>
    public abstract class BossAIBase : EnemyAIBase, IBossMemberView
    {
        [SerializeField, Tooltip("行動中（予備動作〜硬直）は移動を止めるか")]
        private bool _stopMovingWhileActing = true;

        private readonly BossActionPhaseMachine _actionPhase = new();
        private readonly ReactiveProperty<BossMemberStatus> _status = new();

        private bool _isInitialized;
        private bool _isHold;

        public ReadOnlyReactiveProperty<BossMemberStatus> Status => _status;

        protected BossActionPhase ActionPhase => _actionPhase.Phase.CurrentValue;
        protected bool IsActing => ActionPhase != BossActionPhase.Ready;

        /// <summary>台本の指示でその場に待機させられているか</summary>
        protected bool IsHold => _isHold;

        protected override bool IsMovementBlocked =>
            base.IsMovementBlocked || _isHold || (_stopMovingWhileActing && IsActing);

        public override void Init(EnemyData enemyData, int enemyId)
        {
            base.Init(enemyData, enemyId);

            _actionPhase.SetDurations(enemyData.WindupTime, enemyData.ActiveTime, enemyData.RecoveryTime);
            _actionPhase.Phase
                .Subscribe(OnActionPhaseChanged)
                .AddTo(this);

            _isInitialized = true;
            UpdateStatus();
        }

        public bool CommandAction(int actionIndex)
        {
            if (!_status.Value.IsActionable)
            {
                Debug.LogWarning($"[{nameof(BossAIBase)}] {name}: 行動不能のため行動{actionIndex}の命令を無視しました（{_status.Value}）", this);
                return false;
            }

            return _actionPhase.Begin(actionIndex);
        }

        public void SetHold(bool isHold)
        {
            _isHold = isHold;
            ApplyMovementBlock();
        }

        public override void SetStun(bool isStun)
        {
            base.SetStun(isStun);

            // スタンで行動を打ち切る（予備動作中なら攻撃は出ない）
            if (isStun)
            {
                _actionPhase.Cancel();
            }

            UpdateStatus();
        }

        protected override void OnUpdateDeadState()
        {
            base.OnUpdateDeadState();

            _actionPhase.Cancel();
            UpdateStatus();
        }

        protected override void Update()
        {
            base.Update();

            if (IsPause || IsStun || State.Value == EnemyAIState.Dead)
            {
                return;
            }

            // スネークアイズの減速は行動の進行にも掛ける
            _actionPhase.Tick(Time.deltaTime * SpeedMultiplier);
        }

        protected override void BattleState()
        {
            // 攻撃は台本の命令でだけ行う（基底の行動抽選は使わない）
            if (!IsFindDistanceRange())
            {
                SetState(EnemyAIState.Idle);
                return;
            }

            if (IsMovementBlocked)
            {
                return;
            }

            BattleMove();
        }

        /// <summary>戦闘中の移動。待機・行動中・停止中は呼ばれない</summary>
        protected abstract void BattleMove();

        /// <summary>予備動作の開始時（予兆演出など）</summary>
        protected virtual void OnActionWindup(int actionIndex)
        {
        }

        /// <summary>攻撃の開始時。ここで弾を撃つ・判定を出す</summary>
        protected abstract void OnActionActive(int actionIndex);

        /// <summary>攻撃後の硬直の開始時</summary>
        protected virtual void OnActionRecovery(int actionIndex)
        {
        }

        private void OnActionPhaseChanged(BossActionPhase phase)
        {
            switch (phase)
            {
                case BossActionPhase.Windup:
                    OnActionWindup(_actionPhase.ActionIndex);
                    break;
                case BossActionPhase.Active:
                    OnActionActive(_actionPhase.ActionIndex);
                    break;
                case BossActionPhase.Recovery:
                    OnActionRecovery(_actionPhase.ActionIndex);
                    break;
                case BossActionPhase.Ready:
                    break;
            }

            ApplyMovementBlock();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            _status.Value = new BossMemberStatus(
                _isInitialized,
                ActionPhase,
                IsStun,
                State.Value == EnemyAIState.Dead);
        }

        protected virtual void OnDestroy()
        {
            _actionPhase.Dispose();
            _status.Dispose();
        }
    }
}
