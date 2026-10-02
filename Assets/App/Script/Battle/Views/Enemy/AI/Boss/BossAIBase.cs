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
    /// 派生クラスは <see cref="BattleMove"/>（移動）と、攻撃の中身（<see cref="OnActionActive"/> か、持続する攻撃なら <see cref="OnActionActiveUpdate"/>）を実装する。
    /// </summary>
    public abstract class BossAIBase : EnemyAIBase, IBossMemberView
    {
        [SerializeField, Tooltip("行動中（予備動作〜硬直）は移動を止めるか")]
        private bool _stopMovingWhileActing = true;

        private readonly BossActionPhaseMachine _actionPhase = new();
        private readonly ReactiveProperty<BossMemberStatus> _status = new();

        private bool _isInitialized;
        private bool _isHold;

        // 瞬間移動先をNavMesh上へ寄せるときの探索半径（m）。配置先が障害物に掛かっても近くへ出せるよう吹き飛ばしより広く取る
        private const float TeleportSampleDistance = 6f;

        public ReadOnlyReactiveProperty<BossMemberStatus> Status => _status;

        protected BossActionPhase ActionPhase => _actionPhase.Phase.CurrentValue;
        protected bool IsActing => ActionPhase != BossActionPhase.Ready;

        /// <summary>実行中（または直前に実行した）行動の番号</summary>
        protected int CurrentActionIndex => _actionPhase.ActionIndex;

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

            GetActionDurations(actionIndex, out var windup, out var active, out var recovery);
            _actionPhase.SetDurations(windup, active, recovery);
            return _actionPhase.Begin(actionIndex);
        }

        /// <summary>
        /// 行動ごとの各段階の秒数。既定は EnemyMasterData の WindupTime / ActiveTime / RecoveryTime。
        /// 行動によって長さの違うボスは派生クラスで上書きする
        /// </summary>
        protected virtual void GetActionDurations(int actionIndex, out float windup, out float active, out float recovery)
        {
            windup = EnemyData.WindupTime;
            active = EnemyData.ActiveTime;
            recovery = EnemyData.RecoveryTime;
        }

        public void CancelAction()
        {
            _actionPhase.Cancel();
        }

        public void SetHold(bool isHold)
        {
            _isHold = isHold;
            ApplyMovementBlock();
        }

        /// <summary>台本で指定された、プレイヤーに対してつく位置</summary>
        protected BossFormationSlot FormationSlot { get; private set; } = BossFormationSlot.None;

        /// <summary>配置先を横（プレイヤーへ向かう向きと直交する向き）へずらす量（m）</summary>
        protected float FormationLateralOffset { get; private set; }

        public void SetFormation(BossFormationSlot slot, float lateralOffset)
        {
            FormationSlot = slot;
            FormationLateralOffset = lateralOffset;
            OnFormationAssigned(slot);
        }

        /// <summary>配置の指定を受けたとき（既定では何もしない。位置へつく処理は派生クラスが行う）</summary>
        protected virtual void OnFormationAssigned(BossFormationSlot slot)
        {
        }

        /// <summary>
        /// 指定座標へ瞬間移動する（配置のつき直し）。吹き飛ばしの WarpTo と違い、途中の壁で止めない。
        /// 指定座標が NavMesh 外なら近くの NavMesh 上へ寄せ、見つからなければ移動しない
        /// </summary>
        protected bool TeleportTo(Vector3 position)
        {
            if (Agent == null || !UnityEngine.AI.NavMesh.SamplePosition(position, out var hit, TeleportSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
            {
                Debug.LogWarning($"[{nameof(BossAIBase)}] {name}: 瞬間移動先 {position} の近くに NavMesh が無いため移動しませんでした", this);
                return false;
            }

            Agent.Warp(hit.position);
            return true;
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
            var deltaTime = Time.deltaTime * SpeedMultiplier;
            _actionPhase.Tick(deltaTime);

            // 持続のある攻撃（弾幕など）は攻撃の段階の間、毎フレーム処理する
            if (ActionPhase == BossActionPhase.Active)
            {
                OnActionActiveUpdate(_actionPhase.ActionIndex, deltaTime);
            }
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

        /// <summary>攻撃の開始時。ここで弾を撃つ・判定を出す（持続する攻撃は OnActionActiveUpdate を使う）</summary>
        protected virtual void OnActionActive(int actionIndex)
        {
        }

        /// <summary>攻撃の段階の間、毎フレーム呼ばれる（停止・スタン中は呼ばれない）。deltaTime は速度倍率を掛けた値</summary>
        protected virtual void OnActionActiveUpdate(int actionIndex, float deltaTime)
        {
        }

        /// <summary>攻撃後の硬直の開始時</summary>
        protected virtual void OnActionRecovery(int actionIndex)
        {
        }

        /// <summary>行動が終わって待機へ戻ったとき（硬直明け・打ち切り・スタン・撃破のいずれも。予兆の後片付けなど）</summary>
        protected virtual void OnActionFinished(int actionIndex)
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
                    // 初期化時の最初の通知（まだ何も行動していない）は除く
                    if (_isInitialized)
                    {
                        OnActionFinished(_actionPhase.ActionIndex);
                    }

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
