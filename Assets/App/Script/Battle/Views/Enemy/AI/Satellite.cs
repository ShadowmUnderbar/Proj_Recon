using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Satellite : Fire
    {
        [SerializeField] private Transform _body;

        private float RandomHeightMin => 5f;
        private float RandomHeightMax => 12f;
        private float ChaseDistance => EnemyData.AttackDistanceRange * 0.75f;
        private bool IsChaseRange => DistanceSqr > ChaseDistance * ChaseDistance;

        protected override void Awake()
        {
            base.Awake();
            _body.transform.localPosition = new Vector3(0, Random.Range(RandomHeightMin, RandomHeightMax), 0);
        }

        protected override void OnUpdateIdleState()
        {
            base.OnUpdateIdleState();

            Agent.updateRotation = true;
        }

        protected override void OnUpdateBattleState()
        {
            base.OnUpdateBattleState();

            Agent.updateRotation = false;
        }

        protected override void IdleState()
        {
            base.IdleState();

            Agent.SetDestination(PlayerTransform.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            transform.LookAt(PlayerTransform.position, Vector3.up);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

            var targetPos = transform.forward * EnemyData.BattleSpeed * (IsChaseRange ? 1 : -1) * 10f;
            Agent.SetDestination(targetPos);

            MuzzleTransform.LookAt(PlayerTransform.position);
        }
    }
}