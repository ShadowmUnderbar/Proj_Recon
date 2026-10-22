using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
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

            SetAgentDestination(TargetTransform.position);
        }

        protected override void BattleState()
        {
            base.BattleState();

            SetAgentDestination(TargetTransform.position);
        }

        protected override void Attack()
        {
            base.Attack();

            // ポイント粒子はダメージ対象ではないうえ、バッファを埋めてプレイヤーを押し出すため除外する
            var size = Physics.OverlapSphereNonAlloc(transform.position, EnemyData.AttackDistanceRange, _hitResults,
                Physics.AllLayers & ~LayerConstants.PointParticle);

            // 当てるのはプレイヤーだけ。球には自分や周りの敵のヒットボックス・地面も入るので除外し、
            // プレイヤーのコライダーが複数あっても1回だけ当てる（BossTickTock の帯攻撃と同じ扱い）
            var hitPlayerId = int.MinValue;
            for (var i = 0; i < size; i++)
            {
                var hit = _hitResults[i];
                if (!hit.TryGetComponent(out IHitBoxView hitBox) || hitBox.HitBoxType != HitBoxType.Player
                    || hitBox.Id == hitPlayerId)
                {
                    continue;
                }

                hitPlayerId = hitBox.Id;
                hitBox.OnHit(EnemyData.BaseDamage, EnemyData.Id, transform.position, out _);
            }
        }
    }
}