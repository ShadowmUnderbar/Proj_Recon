using System;
using App.Battle.Views.Enemy.Bullet;
using UnityEngine;

namespace App.Battle.Views.Enemy.AI.Boss
{
    /// <summary>
    /// 弾を撃つボスAI。行動番号ごとに弾の数と広がりを変えられる（0: 単発、1: 扇状 など）。
    /// 戦闘中はプレイヤーを向いたまま一定の距離を保つ。
    /// </summary>
    public class BossShooter : BossAIBase
    {
        [Serializable]
        private class ShotPattern
        {
            [SerializeField, Min(1)] private int _bulletCount = 1;

            [SerializeField, Min(0f), Tooltip("両端の弾の間の角度（度）。弾が1発なら使わない")]
            private float _spreadAngle;

            public int BulletCount => _bulletCount;
            public float SpreadAngle => _spreadAngle;
        }

        [SerializeField] private BaseBulletView _bulletPrefab;
        [SerializeField] private Transform _muzzleTransform;

        [SerializeField, Tooltip("行動番号ごとの撃ち方（添字＝台本の行動番号）")]
        private ShotPattern[] _shotPatterns = { new() };

        [SerializeField, Range(0f, 1f), Tooltip("保つ距離（射程に対する割合）")]
        private float _keepDistanceRatio = 0.75f;

        // 距離を保つための移動先を、プレイヤーからどれだけ先に置くか（m）
        private const float MoveTargetDistance = 5f;

        protected override void BattleMove()
        {
            FacePlayer();

            var keepDistance = EnemyData.AttackDistanceRange * _keepDistanceRatio;
            var isTooFar = DistanceSqr > keepDistance * keepDistance;
            var forward = transform.forward * (isTooFar ? 1f : -1f) * MoveTargetDistance;

            SetAgentDestination(transform.position + forward);
        }

        protected override void OnActionWindup(int actionIndex)
        {
            // 予備動作の間に狙いを合わせる（予兆演出は未実装）
            FacePlayer();
        }

        protected override void OnActionActive(int actionIndex)
        {
            if (actionIndex < 0 || actionIndex >= _shotPatterns.Length)
            {
                Debug.LogError($"[{nameof(BossShooter)}] {name}: 行動番号 {actionIndex} の撃ち方が未設定です", this);
                return;
            }

            if (_bulletPrefab == null || _muzzleTransform == null)
            {
                Debug.LogError($"[{nameof(BossShooter)}] {name}: 弾プレハブまたは銃口が未設定です", this);
                return;
            }

            FacePlayer();

            var pattern = _shotPatterns[actionIndex];
            var count = pattern.BulletCount;
            for (var i = 0; i < count; i++)
            {
                // 弾を中央から左右へ等間隔に並べる
                var angle = count == 1 ? 0f : Mathf.Lerp(-pattern.SpreadAngle * 0.5f, pattern.SpreadAngle * 0.5f, i / (count - 1f));
                var rotation = Quaternion.AngleAxis(angle, Vector3.up) * _muzzleTransform.rotation;
                SpawnBullet(new Pose(_muzzleTransform.position, rotation));
            }
        }

        private void SpawnBullet(Pose pose)
        {
            var bullet = Instantiate(_bulletPrefab);
            bullet.Spawn(EnemyId, pose, EnemyData.CreateBulletData(), -1, PlayerTransform);
        }

        private void FacePlayer()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            var direction = PlayerTransform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
