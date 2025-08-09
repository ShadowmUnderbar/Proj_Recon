using App.Battle.Interface.EnemyAI;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Shield : EnemyAIBase
    {
        private Vector3 _moveTargetPosition;

        private Vector3 _aimTargetPosition1;
        private Vector3 _aimTargetPosition2;

        private bool _isMovingToTargetPosition1;

        private float JammingDistance => EnemyData.AttackDistanceRange * 0.5f;

        private float _rotationSpeed;
        private Vector3 _positionOffset;
        private float RandomRotationSpeedMin => 20f;
        private float RandomRotationSpeedMAx => 60f;
        private float RandomPositionOffset => 2.5f;

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

            _aimTargetPosition1 = PlayerTransform.position + -PlayerAimDirection1 * JammingDistance;
            _aimTargetPosition2 = PlayerTransform.position + -PlayerAimDirection2 * JammingDistance;

            if (_isMovingToTargetPosition1)
            {
                Agent.SetDestination(_aimTargetPosition1 + _positionOffset);
            }
            else
            {
                Agent.SetDestination(_aimTargetPosition2 + _positionOffset);
            }

            var direction = PlayerTransform.position - transform.position;
            direction.y = 0f;

            var targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }

        protected override void Attack()
        {
            base.Attack();

            var distance1 = Vector3.Distance(transform.position, _aimTargetPosition1);
            var distance2 = Vector3.Distance(transform.position, _aimTargetPosition2);

            _isMovingToTargetPosition1 = distance1 < distance2;
            _rotationSpeed = Random.Range(RandomRotationSpeedMin, RandomRotationSpeedMAx);

            _positionOffset = new Vector3(
                Random.Range(-RandomPositionOffset, RandomPositionOffset),
                0f,
                Random.Range(-RandomPositionOffset, RandomPositionOffset)
            );
        }
    }
}