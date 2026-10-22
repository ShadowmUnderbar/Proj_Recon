using App.Battle.Data;
using App.Battle.Interface.EnemyAI;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI
{
    public class Shield : EnemyAIBase
    {
        private Vector3 _moveTargetPosition;

        // プレイヤーの照準の先に回りこんで妨害する AI のため、味方は狙わない
        protected override bool CanTargetAllies => false;

        private Vector3 _aimTargetPosition1;
        private Vector3 _aimTargetPosition2;

        private bool _isMovingToTargetPosition1;

        // プレイヤーとの間に保つ距離。照準の先へ回り込むときも、この内側には入らない
        private float JammingDistance => EnemyData.AttackDistanceRange * 0.5f;

        private float _rotationSpeed;
        private Vector3 _positionOffset;
        private float RandomRotationSpeedMin => 20f;
        private float RandomRotationSpeedMAx => 60f;
        private float RandomPositionOffset => 2.5f;

        public override void Init(EnemyData enemyData, int enemyId)
        {
            base.Init(enemyData, enemyId);

            // 最初の攻撃までも振り向けるよう、出現時に旋回速度を決めておく（攻撃のたびに選び直す）
            _rotationSpeed = PickRotationSpeed();
        }

        protected override void OnUpdateIdleState()
        {
            base.OnUpdateIdleState();

            // 進行方向ではなく、常にプレイヤーを向く（FacePlayer）
            Agent.updateRotation = false;
        }

        protected override void OnUpdateBattleState()
        {
            base.OnUpdateBattleState();

            Agent.updateRotation = false;
        }

        protected override void IdleState()
        {
            base.IdleState();

            // 近づくときも一定距離の内側には入らない
            SetAgentDestination(KeepDistanceRoute.NextDestination(
                transform.position, PlayerTransform.position, PlayerTransform.position, JammingDistance));
            KeepOutsideJammingDistance();
            FacePlayer();
        }

        protected override void BattleState()
        {
            base.BattleState();

            if (IsPause)
            {
                return;
            }

            _aimTargetPosition1 = PlayerTransform.position + -PlayerAimDirection1 * JammingDistance;
            _aimTargetPosition2 = PlayerTransform.position + -PlayerAimDirection2 * JammingDistance;

            var goal = (_isMovingToTargetPosition1 ? _aimTargetPosition1 : _aimTargetPosition2) + _positionOffset;

            // 反対側の射線へ移るときもプレイヤーのそばを横切らず、一定距離を保って外周を回り込む
            SetAgentDestination(KeepDistanceRoute.NextDestination(
                transform.position, PlayerTransform.position, goal, JammingDistance));
            KeepOutsideJammingDistance();

            FacePlayer();
        }

        /// <summary>
        /// 速度の慣性やプレイヤーの接近で一定距離の内側に入ったら、外周まで押し戻し、プレイヤーへ向かう速度を消す
        /// </summary>
        private void KeepOutsideJammingDistance()
        {
            if (IsPause || !Agent.isOnNavMesh)
            {
                return;
            }

            if (!KeepDistanceRoute.TryGetPushOut(transform.position, PlayerTransform.position, JammingDistance,
                    out var pushOut, out var outward))
            {
                return;
            }

            Agent.Move(pushOut);

            var velocity = Agent.velocity;
            var outwardSpeed = Vector3.Dot(velocity, outward);
            if (outwardSpeed < 0f)
            {
                Agent.velocity = velocity - outward * outwardSpeed;
            }
        }

        private float PickRotationSpeed() => Random.Range(RandomRotationSpeedMin, RandomRotationSpeedMAx);

        /// <summary>設定された旋回速度で、プレイヤーの位置へ向き直る（停止中は向きも止める）</summary>
        private void FacePlayer()
        {
            if (IsPause)
            {
                return;
            }

            var direction = PlayerTransform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

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
            _rotationSpeed = PickRotationSpeed();

            _positionOffset = new Vector3(
                Random.Range(-RandomPositionOffset, RandomPositionOffset),
                0f,
                Random.Range(-RandomPositionOffset, RandomPositionOffset)
            );
        }
    }
}