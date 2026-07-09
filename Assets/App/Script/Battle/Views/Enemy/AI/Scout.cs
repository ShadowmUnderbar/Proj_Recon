using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Scout : Fire
    {
        [SerializeField] private LineRenderer lineRenderer;

        private float RandomMoveRange => 0.5f;

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
        }

        protected override void Attack()
        {
            base.Attack();

            var dirAway = (transform.position - PlayerTransform.position).normalized +
                          transform.right * Random.Range(-RandomMoveRange, RandomMoveRange);
            var candidate = PlayerTransform.position + dirAway * EscapeDistance;

            SetAgentDestination(candidate);
        }
    }
}