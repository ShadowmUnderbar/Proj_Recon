using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Scout : Fire
    {
        [SerializeField] private LineRenderer lineRenderer;

        private float RandomMoveRange => 0.5f;
        private float RandomDistance => Random.Range(-RandomMoveRange, RandomMoveRange);

        private static float EscapeDistance => 30f;

        protected override void Update()
        {
            base.Update();

            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, lineRenderer.transform.position);
            lineRenderer.SetPosition(1,
                lineRenderer.transform.position + lineRenderer.transform.forward * EnemyData.AttackDistanceRange);
        }

        protected override void IdleState()
        {
            base.IdleState();

            Agent.SetDestination(PlayerPose.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            transform.LookAt(PlayerPose.position, Vector3.up);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        protected override void Attack()
        {
            base.Attack();

            var dirAway = (transform.position - PlayerPose.position).normalized + transform.right * RandomDistance;
            var candidate = PlayerPose.position + dirAway * EscapeDistance;

            Agent.SetDestination(candidate);
        }
    }
}