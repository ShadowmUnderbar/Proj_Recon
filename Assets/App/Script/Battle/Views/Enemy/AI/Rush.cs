using App.Battle.Interface;
using App.Battle.Interface.EnemyAI;
using UnityEngine;

namespace App.Battle.Views.EnemyAI
{
    public class Rush : EnemyAIBase
    {
        private Collider[] _hitResults = new Collider[10];

        protected override void IdleState()
        {
            base.IdleState();

            SetAgentDestination(PlayerTransform.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            SetAgentDestination(PlayerTransform.position);
        }

        protected override void Attack()
        {
            base.Attack();

            var size = Physics.OverlapSphereNonAlloc(transform.position, EnemyData.AttackDistanceRange, _hitResults);

            for (var i = 0; i < size; i++)
            {
                var hit = _hitResults[i];
                if (!hit.TryGetComponent(out IHitBoxView hitBox))
                {
                    return;
                }

                hitBox.OnHit(EnemyData.BaseDamage, EnemyData.Id, transform.position, out _);
            }
        }
    }
}