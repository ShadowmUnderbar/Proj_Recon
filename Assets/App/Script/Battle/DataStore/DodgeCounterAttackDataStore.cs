using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 回避時跳ね返し攻撃の実行時状態。
    /// 回避中に巻き込んだ敵弾・敵を記録し、回避終了時に「終了地点を頂点とした扇形範囲」への
    /// 攻撃対象・ダメージ・接触敵の押し出し先を算出する。
    /// </summary>
    public class DodgeCounterAttackDataStore : IDodgeCounterAttackDataStore
    {
        // 通常対象の最大射程（m）
        private const float AttackRange = 15f;

        // 発射方向を中心とした左右の許容角度（度）。範囲は左右合計20度の扇形になる
        private const float AttackHalfAngle = 10f;

        // 接触1件（敵弾・敵の区別なし）あたりのダメージ加算値
        private const float DamagePerContact = 0.1f;

        // 接触した敵を回避終了地点から回避方向へ押し出す距離（m）
        private const float PushDistance = 2f;

        // 複数の敵を押し出すときに重ならないよう左右へずらす間隔（m）
        private const float PushSpacing = 1f;

        // 遮蔽判定に使う高さ（m）。回避終了地点・敵Poseはいずれも足元基準のため胴体あたりで見通しを見る
        private const float SightHeight = 1f;

        // 被弾を「接触」と見なすプレイヤーからの距離（m）。
        // 近接攻撃の間合いを想定した値で、遠方から届く爆風を接触扱いしないための上限
        private const float ContactRange = 3f;

        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        // 同一の回避中に同じ対象を重複カウントしないためのId集合
        private readonly HashSet<int> _contactedProjectileIds = new();
        private readonly HashSet<int> _contactedEnemyIdSet = new();

        // 接触した敵は押し出し・強制対象化で列挙するため、順序付きでも保持する
        private readonly List<int> _contactedEnemyIds = new();

        // 攻撃対象の作業バッファ（毎回のアロケーションを避けるため使い回す）
        private readonly List<int> _targetEnemyIds = new();

        [Inject]
        public DodgeCounterAttackDataStore(
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _enemyDataStore = enemyDataStore;
        }

        public int ContactedProjectileCount => _contactedProjectileIds.Count;

        public int ContactedEnemyCount => _contactedEnemyIds.Count;

        public IReadOnlyList<int> ContactedEnemyIds => _contactedEnemyIds;

        public void RegisterProjectileContact(int projectileId)
        {
            // Idを持たない攻撃（弾以外）は識別できないためカウントしない
            if (projectileId == 0)
            {
                return;
            }

            _contactedProjectileIds.Add(projectileId);
        }

        public void RegisterEnemyContact(int enemyId)
        {
            if (!_contactedEnemyIdSet.Add(enemyId))
            {
                return;
            }

            _contactedEnemyIds.Add(enemyId);
        }

        public bool IsWithinContactRange(int enemyId, Vector3 playerPosition)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return false;
            }

            var toEnemy = enemyData.Pose.position - playerPosition;
            toEnemy.y = 0f;

            return toEnemy.sqrMagnitude <= ContactRange * ContactRange;
        }

        public void ResetContacts()
        {
            _contactedProjectileIds.Clear();
            _contactedEnemyIdSet.Clear();
            _contactedEnemyIds.Clear();
        }

        public float CalcDamage()
        {
            // 各フォームの弾ダメージ（アップグレード・バフ込み）の合計を基礎ダメージとする。
            // フォーカス補正は乗せない（跳ね返しは射撃ではないため非フォーカス基準）。
            // マージ／ワルツが未解放でも合算する（仕様として3種合計を基礎火力に据えている）
            var damage =
                _playerBulletParameterDataStore.GetBulletData(ShotType.Normal, AimFocusType.NotFocus).Damage +
                _playerBulletParameterDataStore.GetBulletData(ShotType.Merge, AimFocusType.NotFocus).Damage +
                _playerBulletParameterDataStore.GetBulletData(ShotType.Waltz, AimFocusType.NotFocus).Damage;

            // 巻き込んだ数だけ加算（仕様どおりの加算補正）
            damage += (ContactedProjectileCount + ContactedEnemyCount) * DamagePerContact;

            return damage;
        }

        public float GetTracerWidth()
        {
            // 見た目をノーマルショットのレイに揃える（HitRange強化も同じように反映される）
            return _playerBulletParameterDataStore.GetBulletData(ShotType.Normal, AimFocusType.NotFocus).Size;
        }

        public IReadOnlyList<int> GetTargetEnemyIds(Vector3 origin, Vector3 direction)
        {
            _targetEnemyIds.Clear();

            // 回避中に接触した敵は扇形範囲外でも必ず対象にする（撃破済みは除く）
            foreach (var enemyId in _contactedEnemyIds)
            {
                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var contactedEnemy))
                {
                    continue;
                }

                if (contactedEnemy.IsDead)
                {
                    continue;
                }

                _targetEnemyIds.Add(enemyId);
            }

            var forward = direction;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0f)
            {
                return _targetEnemyIds;
            }

            forward.Normalize();

            var enemies = _enemyDataStore.Enemies;

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];

                if (enemy.IsDead)
                {
                    continue;
                }

                if (_contactedEnemyIdSet.Contains(enemy.Id))
                {
                    continue;
                }

                if (!IsInAttackSector(origin, forward, enemy.Pose.position))
                {
                    continue;
                }

                _targetEnemyIds.Add(enemy.Id);
            }

            return _targetEnemyIds;
        }

        public Vector3 GetPushPosition(Vector3 origin, Vector3 direction, int index, int count)
        {
            var forward = direction;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0f)
            {
                return origin;
            }

            forward.Normalize();

            var pushPosition = origin + forward * PushDistance;

            if (count <= 1)
            {
                return pushPosition;
            }

            // 同じ座標へ重ねると押し合いでジッターするため、回避方向に対して左右へ等間隔に散らす
            var right = Vector3.Cross(Vector3.up, forward);
            var offset = (index - (count - 1) * 0.5f) * PushSpacing;

            return pushPosition + right * offset;
        }

        /// <summary>
        /// origin を頂点・forward を中心軸とした扇形（半径 AttackRange / 左右 AttackHalfAngle）に
        /// targetPosition が入っているかを水平面で判定する。
        /// 壁越しの敵は攻撃対象にしないため、範囲内でも見通しが遮られていれば false を返す。
        /// </summary>
        private static bool IsInAttackSector(Vector3 origin, Vector3 forward, Vector3 targetPosition)
        {
            var toTarget = targetPosition - origin;
            toTarget.y = 0f;

            var sqrDistance = toTarget.sqrMagnitude;

            if (sqrDistance > AttackRange * AttackRange)
            {
                return false;
            }

            // 起点と同一座標の敵は角度が定義できないため範囲内として扱う
            if (sqrDistance <= 0f)
            {
                return true;
            }

            if (Vector3.Angle(forward, toTarget.normalized) > AttackHalfAngle)
            {
                return false;
            }

            return !IsSightBlocked(origin, targetPosition);
        }

        /// <summary>
        /// 回避終了地点から対象への見通しが壁（フィールド）に遮られているかを返す。
        /// </summary>
        private static bool IsSightBlocked(Vector3 origin, Vector3 targetPosition)
        {
            var start = origin + Vector3.up * SightHeight;
            var end = targetPosition + Vector3.up * SightHeight;

            return Physics.Linecast(start, end, LayerMasks.FieldLayer);
        }
    }
}
