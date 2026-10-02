using App.Battle.Data;
using App.Battle.Views.Enemy.Bullet;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI.Boss
{
    /// <summary>
    /// プレイヤーの上下左右（ワールドの軸）のいずれかについて動く、二人組ボス用のAI。
    /// 台本の配置（CrossFormation）を受けると、プレイヤーからその方向へ一定距離の位置へ瞬間移動する。
    /// 行動していない間は、その位置で距離を保ったままプレイヤーを追う。
    /// 行動（弾幕）中は、縦方向（上下）にいれば横へ、横方向（左右）にいれば縦へ、プレイヤーに合わせて軸に沿って動き、
    /// 移動方向と直交する向き（プレイヤーの側）へ弾を連射する。弾幕の長さは EnemyMasterData の ActiveTime。
    /// プレハブでは基底の「行動中は移動を止める」を切っておくこと（弾幕中も動くため）。
    /// </summary>
    public class BossAxisBarrage : BossAIBase
    {
        [SerializeField] private BaseBulletView _bulletPrefab;
        [SerializeField] private Transform _muzzleTransform;

        [SerializeField, Min(1f), Tooltip("プレイヤーとの距離（m）。配置のつき直し・追跡でこの距離を保つ")]
        private float _keepDistance = 15f;

        [SerializeField, Min(0.02f), Tooltip("弾幕で弾を撃つ間隔（秒）")]
        private float _fireInterval = 0.2f;

        // 弾幕中に保つ座標（縦方向にいればZ、横方向にいればX）。行動開始時の位置で固定し、軸に沿ってだけ動く
        private float _barrageLine;

        // 次の弾までの残り時間（秒）
        private float _fireTimer;

        protected override void OnFormationAssigned(BossFormationSlot slot)
        {
            if (slot == BossFormationSlot.None || PlayerTransform == null)
            {
                return;
            }

            TeleportTo(GetFormationPosition());
            FaceFireDirection();
        }

        protected override void BattleMove()
        {
            if (FormationSlot == BossFormationSlot.None)
            {
                return;
            }

            FaceFireDirection();

            SetAgentDestination(IsActing ? GetBarragePosition() : GetFormationPosition());
        }

        protected override void OnActionWindup(int actionIndex)
        {
            _barrageLine = FormationSlot.IsVertical() ? transform.position.z : transform.position.x;

            // 攻撃の段階に入った最初のフレームで1発目を撃つ
            _fireTimer = 0f;
        }

        protected override void OnActionActiveUpdate(int actionIndex, float deltaTime)
        {
            if (FormationSlot == BossFormationSlot.None)
            {
                return;
            }

            _fireTimer -= deltaTime;
            while (_fireTimer <= 0f)
            {
                Fire();
                _fireTimer += _fireInterval;
            }
        }

        private void Fire()
        {
            if (_bulletPrefab == null || _muzzleTransform == null)
            {
                Debug.LogError($"[{nameof(BossAxisBarrage)}] {name}: 弾プレハブまたは銃口が未設定です", this);
                return;
            }

            var pose = new Pose(_muzzleTransform.position, Quaternion.LookRotation(GetFireDirection(), Vector3.up));
            var bullet = Instantiate(_bulletPrefab);
            bullet.Spawn(EnemyId, pose, EnemyData.CreateBulletData(), -1, PlayerTransform);
        }

        /// <summary>配置先（プレイヤーからその方向へ一定距離）</summary>
        private Vector3 GetFormationPosition()
        {
            return PlayerTransform.position + FormationSlot.ToDirection() * _keepDistance;
        }

        /// <summary>弾幕中の移動先（自分の軸の線上で、プレイヤーと並ぶ位置）</summary>
        private Vector3 GetBarragePosition()
        {
            var player = PlayerTransform.position;
            return FormationSlot.IsVertical()
                ? new Vector3(player.x, transform.position.y, _barrageLine)
                : new Vector3(_barrageLine, transform.position.y, player.z);
        }

        /// <summary>弾を撃つ向き（移動方向と直交し、プレイヤーの側を向く）</summary>
        private Vector3 GetFireDirection()
        {
            return -FormationSlot.ToDirection();
        }

        private void FaceFireDirection()
        {
            transform.rotation = Quaternion.LookRotation(GetFireDirection(), Vector3.up);
        }
    }
}
