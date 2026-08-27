using App.Battle.Data;
using R3;
using UnityEngine;
using UnityEngine.AI;

namespace App.Battle.Interface.EnemyAI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyAIBase : MonoBehaviour
    {
        public int EnemyId { get; private set; }
        protected bool CanAttack { get; set; } = true;
        protected bool IsPause { get; set; } = false;

        /// <summary>スタン中か（メデューサ）。移動・行動抽選・攻撃をすべて停止する</summary>
        protected bool IsStun { get; private set; } = false;

        // 移動速度と行動抽選速度に掛かる倍率（スネークアイズ）
        private float _speedMultiplier = 1f;

        // ワープ先をNavMesh上へ寄せるときの探索半径（m）
        private const float WarpSampleDistance = 2f;

        protected EnemyData EnemyData;
        protected NavMeshAgent Agent;
        protected Transform PlayerTransform;
        protected Vector3 PlayerAimDirection1;
        protected Vector3 PlayerAimDirection2;
        protected ReactiveProperty<EnemyAIState> State { get; } = new(EnemyAIState.None);
        protected float DistanceSqr => Vector3.SqrMagnitude(transform.position - PlayerTransform.position);
        protected float LastAttackTime;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            if (Agent == null)
            {
                Debug.LogError($"{gameObject.name} にNavMeshAgentが不足");
            }
        }

        public virtual void Init(EnemyData enemyData, int enemyId)
        {
            EnemyId = enemyId;
            EnemyData = enemyData;
            ApplyAgentSpeed(enemyData.IdleSpeed);
            Agent.angularSpeed = 360f;
            Agent.updateRotation = false;

            SetState(EnemyAIState.Idle);

            State.Subscribe(OnUpdateState)
                .AddTo(this);
        }

        protected virtual void OnUpdateState(EnemyAIState state)
        {
            switch (state)
            {
                case EnemyAIState.Idle:
                    OnUpdateIdleState();
                    break;
                case EnemyAIState.Battle:
                    OnUpdateBattleState();
                    break;
                case EnemyAIState.Dead:
                    OnUpdateDeadState();
                    break;
                case EnemyAIState.None:
                    break;
            }
        }

        protected virtual void OnUpdateIdleState()
        {
            ApplyAgentSpeed(EnemyData.IdleSpeed);
        }

        protected virtual void OnUpdateBattleState()
        {
            ApplyAgentSpeed(EnemyData.BattleSpeed);
        }

        /// <summary>
        /// 基礎速度に速度倍率を掛けてNavMeshAgentへ反映する。
        /// </summary>
        private void ApplyAgentSpeed(float baseSpeed)
        {
            if (Agent == null)
            {
                return;
            }

            Agent.speed = baseSpeed * _speedMultiplier;
            Agent.acceleration = baseSpeed * 2f * _speedMultiplier;
        }

        /// <summary>
        /// 現在のステートに応じた基礎速度へ速度倍率を再適用する。
        /// </summary>
        private void ApplyCurrentAgentSpeed()
        {
            if (EnemyData == null)
            {
                return;
            }

            ApplyAgentSpeed(State.Value == EnemyAIState.Battle ? EnemyData.BattleSpeed : EnemyData.IdleSpeed);
        }

        protected virtual void OnUpdateDeadState()
        {
        }

        protected virtual void Update()
        {
            if (Agent == null)
            {
                return;
            }

            // スタン中は行動・行動抽選を一切行わない（死亡演出だけは進める）
            if (IsStun && State.Value != EnemyAIState.Dead)
            {
                return;
            }

            // 停止中は行動抽選も進めない（溜まった分で解除直後に一斉攻撃するのを防ぐ）
            if (CanAttack && !IsPause)
            {
                // 速度倍率は行動抽選の進行速度にも掛かる
                LastAttackTime += Time.deltaTime * _speedMultiplier;
            }

            switch (State.Value)
            {
                case EnemyAIState.Idle:
                    IdleState();
                    break;
                case EnemyAIState.Battle:
                    BattleState();
                    break;
                case EnemyAIState.Dead:
                    DeadState();
                    break;
                case EnemyAIState.None:
                    break;
            }
        }

        protected virtual void IdleState()
        {
            if (!IsFindDistanceRange())
            {
                return;
            }

            SetState(EnemyAIState.Battle);
        }

        protected virtual void BattleState()
        {
            if (!IsFindDistanceRange())
            {
                SetState(EnemyAIState.Idle);
                return;
            }

            if (!IsAttackDistanceRange())
            {
                return;
            }

            if (!IsAttackInterval())
            {
                return;
            }

            if (IsPause)
            {
                return;
            }

            if (!CanAttack)
            {
                return;
            }

            Attack();
        }

        protected virtual void DeadState()
        {
        }

        protected virtual bool IsAttackInterval()
        {
            if (LastAttackTime <= EnemyData.AttackInterval + Random.Range(-0.2f, 0.2f))
            {
                return false;
            }

            return true;
        }

        protected virtual void Attack()
        {
            LastAttackTime = 0f;
        }

        public void SetPlayerTransform(Transform playerTransform)
        {
            PlayerTransform = playerTransform;
        }

        public void SetPlayerAimDirection(Vector3 aimDir1, Vector3 aimDir2)
        {
            PlayerAimDirection1 = aimDir1;
            PlayerAimDirection2 = aimDir2;
        }

        public void SetState(EnemyAIState state)
        {
            State.Value = state;
        }

        public virtual bool IsFindDistanceRange()
        {
            return DistanceSqr <= EnemyData.FindDistanceRange * EnemyData.FindDistanceRange;
        }

        public virtual bool IsAttackDistanceRange()
        {
            return DistanceSqr <= EnemyData.AttackDistanceRange * EnemyData.AttackDistanceRange;
        }

        protected void SetAgentDestination(Vector3 position)
        {
            if (Agent == null || !Agent.isOnNavMesh)
            {
                return;
            }

            Agent.SetDestination(position);
        }

        public virtual void SetPause(bool isPause)
        {
            IsPause = isPause;
            ApplyMovementBlock();
        }

        /// <summary>
        /// 移動・行動抽選の速度倍率を設定する（スネークアイズ）。
        /// </summary>
        public virtual void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
            ApplyCurrentAgentSpeed();
        }

        /// <summary>
        /// 指定座標へワープする（回避時跳ね返し攻撃で回避方向へ押し出す）。
        /// NavMeshAgentは直接transformを動かすと経路が壊れるため Warp を使い、
        /// 指定座標がNavMesh外の場合は最も近いNavMesh上の地点へ寄せる。
        /// </summary>
        public virtual void WarpTo(Vector3 position)
        {
            if (Agent == null)
            {
                return;
            }

            if (!NavMesh.SamplePosition(position, out var hit, WarpSampleDistance, NavMesh.AllAreas))
            {
                return;
            }

            var destination = hit.position;

            // 壁の向こうへ飛ばさないよう、現在地から目的地までNavMesh上を辿り、
            // 遮られた場合はその地点で止める
            if (NavMesh.Raycast(transform.position, destination, out var navMeshHit, NavMesh.AllAreas))
            {
                destination = navMeshHit.position;
            }

            Agent.Warp(destination);
        }

        /// <summary>
        /// スタン状態を設定する（メデューサ）。
        /// スタン開始時は進行中の行動抽選をキャンセルしてその場で停止する。
        /// </summary>
        public virtual void SetStun(bool isStun)
        {
            IsStun = isStun;

            if (isStun)
            {
                // 直前の行動抽選をキャンセルする（解除後は抽選をやり直す）
                LastAttackTime = 0f;
            }

            ApplyMovementBlock();
        }

        /// <summary>
        /// ポーズ・スタンいずれかの状態に応じてエージェントの移動を停止/再開する。
        /// </summary>
        private void ApplyMovementBlock()
        {
            // NavMesh未配置のエージェントにisStoppedを設定するとエラーログが出る（SetAgentDestinationと同じガード）。
            // 論理状態(IsPause/IsStun)は先に更新済みなので、配置後のAI更新はそちらに従う
            if (Agent == null || !Agent.isOnNavMesh)
            {
                return;
            }

            var isBlocked = IsPause || IsStun;
            Agent.isStopped = isBlocked;

            if (isBlocked)
            {
                Agent.velocity = Vector3.zero;
            }
        }
    }
}