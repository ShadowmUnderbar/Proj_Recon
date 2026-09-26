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

        /// <summary>射程外から近づくとき、射程ぎりぎりで止まって攻撃判定に入らないのを防ぐための余裕[m]</summary>
        private static float ApproachMargin => 1f;

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

            // 索敵内だが射程外なら、プレイヤーへ向かって射程に入る地点まで近づく
            if (State.Value == EnemyAIState.Battle && !IsAttackDistanceRange())
            {
                ApproachToAttackRange();
            }
        }

        /// <summary>
        /// プレイヤーから射程距離だけ手前の地点を目的地にする。
        /// プレイヤー位置そのものを目的地にすると射程に入っても止まらず突っ込むため、
        /// 射程の内側に少し入った地点で止まるようにしている
        /// </summary>
        private void ApproachToAttackRange()
        {
            var toSelf = transform.position - PlayerTransform.position;
            toSelf.y = 0f;

            var stopDistance = Mathf.Max(0f, EnemyData.AttackDistanceRange - ApproachMargin);
            var destination = PlayerTransform.position + toSelf.normalized * stopDistance;

            SetAgentDestination(destination);
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