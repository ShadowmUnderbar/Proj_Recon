using App.Battle.Data;
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

            // base側で索敵に成功すると戦闘状態へ遷移し、その時点の自分の位置を目的地にする。
            // その後にプレイヤー位置で上書きすると戦闘速度で突っ込んでしまうため、待機のままのときだけ追う
            if (State.Value != EnemyAIState.Idle)
            {
                return;
            }

            SetAgentDestination(PlayerTransform.position);
        }

        protected override void OnUpdateBattleState()
        {
            base.OnUpdateBattleState();

            // 待機中に向かっていたプレイヤー位置を目的地に残したまま戦闘速度に切り替わると、
            // 次の攻撃（逃げ先の設定）までプレイヤーへ突っ込んでしまう。
            // 戦闘に入った時点の自分の位置を目的地にしてその場で止める
            SetAgentDestination(transform.position);
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