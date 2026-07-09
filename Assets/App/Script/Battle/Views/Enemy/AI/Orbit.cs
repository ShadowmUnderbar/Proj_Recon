using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Orbit : Fire
    {
        private bool _isRotateRight;

        private float ChaseDistance => EnemyData.AttackDistanceRange * 0.75f;

        private bool IsChaseRange => DistanceSqr > ChaseDistance * ChaseDistance;

        protected override void Awake()
        {
            base.Awake();

            _isRotateRight = Random.Range(0, 2) == 0;
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

            SetAgentDestination(PlayerTransform.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            if (IsPause)
            {
                return;
            }

            transform.LookAt(PlayerTransform.position, Vector3.up);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

            var targetPos = transform.right * EnemyData.BattleSpeed * (_isRotateRight ? 1 : -1) * 10f;
            targetPos += transform.forward * EnemyData.BattleSpeed * (IsChaseRange ? 1 : -1) * 10f;

            SetAgentDestination(targetPos);
        }
    }
}