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
            Agent.speed = enemyData.IdleSpeed;
            Agent.acceleration = enemyData.IdleSpeed * 2f;
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

            if (CanAttack)
            {
                LastAttackTime += Time.deltaTime;
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
            if (Agent == null)
            {
                return;
            }

            Agent.isStopped = isPause;

            if (isPause)
            {
                Agent.velocity = Vector3.zero;
            }
        }
    }
}