using App.Common.Views;
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

            var start = lineRenderer.transform.position;
            var end = start + lineRenderer.transform.forward * EnemyData.AttackDistanceRange;

            // カーブ有効時は途中に点を足す。2点のままだと両端しか沈まず、間が浮く
            CurvedWorldLine.SetLine(lineRenderer, start, end);
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