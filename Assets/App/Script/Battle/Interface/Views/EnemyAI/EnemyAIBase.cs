using App.Battle.Data;
using R3;
using UnityEngine;
using UnityEngine.AI;

namespace App.Battle.Interface.EnemyAI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyAIBase : MonoBehaviour
    {
        protected EnemyData EnemyData;
        protected NavMeshAgent Agent;
        protected Pose PlayerPose;
        protected ReactiveProperty<EnemyAIState> State { get; } = new(EnemyAIState.None);
        protected float DistanceSqr => Vector3.SqrMagnitude(transform.position - PlayerPose.position);
        protected float LastAttackTime;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            if (Agent == null)
            {
                Debug.LogError($"{gameObject.name} にNavMeshAgentが不足");
            }
        }

        public void Init(EnemyData enemyData)
        {
            EnemyData = enemyData;
            Agent.speed = enemyData.IdleSpeed;
            Agent.acceleration = enemyData.IdleSpeed * 2f;
            Agent.angularSpeed = 360f;
            Agent.stoppingDistance = enemyData.AttackDistanceRange * 0.5f;
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
            Agent.speed = EnemyData.IdleSpeed;
            Agent.acceleration = EnemyData.IdleSpeed * 2f;
        }

        protected virtual void OnUpdateBattleState()
        {
            Agent.speed = EnemyData.BattleSpeed;
            Agent.acceleration = EnemyData.BattleSpeed * 2f;
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

            LastAttackTime += Time.deltaTime;

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

            Attack();
        }

        protected virtual void DeadState()
        {
        }

        protected virtual bool IsAttackInterval()
        {
            if (LastAttackTime <= EnemyData.AttackInterval)
            {
                return false;
            }

            return true;
        }

        protected virtual void Attack()
        {
            LastAttackTime = 0f;
        }

        public void SetPlayerPose(Pose playerPose)
        {
            PlayerPose = playerPose;
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
    }
}