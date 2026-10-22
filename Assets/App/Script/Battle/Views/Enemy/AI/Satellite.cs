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
            Agent.baseOffset = Random.Range(RandomHeightMin, RandomHeightMax);
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

            SetAgentDestination(TargetTransform.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            if (IsPause)
            {
                return;
            }

            transform.LookAt(TargetTransform.position, Vector3.up);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

            // 自分の位置を基準にした目的地にする（基準を足さないとワールド原点へ向かってしまう）
            var targetPos = transform.position + transform.forward * EnemyData.BattleSpeed * (IsChaseRange ? 1 : -1) * 10f;
            SetAgentDestination(targetPos);

            MuzzleTransform.LookAt(TargetTransform.position);
        }
    }
}