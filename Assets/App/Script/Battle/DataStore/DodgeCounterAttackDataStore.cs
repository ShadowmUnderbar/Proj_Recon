using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using App.Common.Data;
using R3;
using UnityEngine;
using VContainer;

namespace App.Battle.DataStore
{
    /// <summary>
    /// 回避時跳ね返し攻撃の実行時状態。
    /// 回避中に巻き込んだ敵弾・敵を記録し、回避終了時に「終了地点を頂点とした扇形範囲」への
    /// 攻撃対象・ダメージ・接触敵の押し出し先を算出する。
    /// </summary>
    public class DodgeCounterAttackDataStore : IDodgeCounterAttackDataStore, IDisposable
    {
        // 攻撃範囲・ダメージ加算・押し出し・接触判定の調整値（インスペクタで調整する）
        private readonly DodgeCounterAttackConfig _config;

        private readonly IPlayerBulletParameterDataStore _playerBulletParameterDataStore;
        private readonly IEnemyDataStore _enemyDataStore;

        // 同一の回避中に同じ対象を重複カウントしないためのId集合
        private readonly HashSet<int> _contactedProjectileIds = new();
        private readonly HashSet<int> _contactedEnemyIdSet = new();

        // 接触した敵は押し出し・強制対象化で列挙するため、順序付きでも保持する
        private readonly List<int> _contactedEnemyIds = new();

        // 攻撃対象の作業バッファ（毎回のアロケーションを避けるため使い回す）
        private readonly List<int> _targetEnemyIds = new();

        // 扇形と直線で同じ敵を二重に攻撃しないための対象Id集合
        private readonly HashSet<int> _targetEnemyIdSet = new();

        // 直線判定の対象の作業バッファ
        private readonly List<int> _lineTargetEnemyIds = new();

        private readonly Subject<int> _onEnemyContacted = new();
        public Observable<int> OnEnemyContacted => _onEnemyContacted;

        // 接触でスタンさせた敵Id → 残りスタン時間。接触記録とは独立して管理する
        // （スタンは回避終了後のフリーズ明けまで続くため）
        private readonly Dictionary<int, float> _stunRemainingTime = new();

        // 残り時間が切れた敵Idの作業バッファ（辞書の列挙中に削除できないため）
        private readonly List<int> _expiredStunEnemyIds = new();

        // スタン継続中の敵Idの一時退避（辞書の列挙中に値を書き換えられないため）
        private readonly List<int> _activeStunEnemyIds = new();

        [Inject]
        public DodgeCounterAttackDataStore(
            DodgeCounterAttackConfig config,
            IPlayerBulletParameterDataStore playerBulletParameterDataStore,
            IEnemyDataStore enemyDataStore
        )
        {
            _config = config;
            _playerBulletParameterDataStore = playerBulletParameterDataStore;
            _enemyDataStore = enemyDataStore;
        }

        public int ContactedProjectileCount => _contactedProjectileIds.Count;

        public int ContactedEnemyCount => _contactedEnemyIds.Count;

        public IReadOnlyList<int> ContactedEnemyIds => _contactedEnemyIds;

        public bool HasContact => ContactedProjectileCount + ContactedEnemyCount > 0;

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

            // 接触した瞬間のスタン・吹き飛ばしを起動する
            _onEnemyContacted.OnNext(enemyId);
        }

        public bool IsWithinContactRange(int enemyId, Vector3 playerPosition)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return false;
            }

            var toEnemy = enemyData.Pose.position - playerPosition;
            toEnemy.y = 0f;

            return toEnemy.sqrMagnitude <= _config.ContactRange * _config.ContactRange;
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
            damage += (ContactedProjectileCount + ContactedEnemyCount) * _config.DamagePerContact;

            return damage;
        }

        public float GetTracerWidth()
        {
            // 見た目をノーマルショットのレイに揃える（HitRange強化も同じように反映される）
            return _playerBulletParameterDataStore.GetBulletData(ShotType.Normal, AimFocusType.NotFocus).Size;
        }

        public float LineAttackDistance => _config.LineAttackDistance;

        public float GetLineAttackRadius()
        {
            // ノーマル／マージ／ワルツの当たり判定サイズ合計に基礎半径を足す（HitRange強化も反映される）
            var size =
                _playerBulletParameterDataStore.GetBulletData(ShotType.Normal, AimFocusType.NotFocus).Size +
                _playerBulletParameterDataStore.GetBulletData(ShotType.Merge, AimFocusType.NotFocus).Size +
                _playerBulletParameterDataStore.GetBulletData(ShotType.Waltz, AimFocusType.NotFocus).Size;

            return _config.LineAttackBaseRadius + size;
        }

        public IReadOnlyList<int> GetLineTargetEnemyIds(IReadOnlyList<int> lineHitEnemyIds)
        {
            _lineTargetEnemyIds.Clear();

            for (var i = 0; i < lineHitEnemyIds.Count; i++)
            {
                var enemyId = lineHitEnemyIds[i];

                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData) || enemyData.IsDead)
                {
                    continue;
                }

                // 扇形範囲の対象（＝既に攻撃済み）と重複させない
                if (!_targetEnemyIdSet.Add(enemyId))
                {
                    continue;
                }

                _lineTargetEnemyIds.Add(enemyId);
            }

            return _lineTargetEnemyIds;
        }

        public IReadOnlyList<int> GetTargetEnemyIds(Vector3 origin, Vector3 direction)
        {
            _targetEnemyIds.Clear();
            _targetEnemyIdSet.Clear();

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

                if (!_targetEnemyIdSet.Add(enemyId))
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

                _targetEnemyIdSet.Add(enemy.Id);
                _targetEnemyIds.Add(enemy.Id);
            }

            return _targetEnemyIds;
        }

        public float GetContactStunDuration(float knockBackDuration)
        {
            // 吹き飛ばしが終わってからフリーズが明けるまで固まったままにする
            return Mathf.Max(0f, knockBackDuration) + Mathf.Max(0f, _config.FreezeDuration);
        }

        public void RegisterStun(int enemyId, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            // 短い指定で上書きしないよう、残り時間の長い方を採用する
            if (_stunRemainingTime.TryGetValue(enemyId, out var remaining))
            {
                duration = Mathf.Max(remaining, duration);
            }

            _stunRemainingTime[enemyId] = duration;
        }

        public IReadOnlyList<int> UpdateStunTimers(float deltaTime)
        {
            _expiredStunEnemyIds.Clear();

            if (_stunRemainingTime.Count <= 0)
            {
                return _expiredStunEnemyIds;
            }

            foreach (var enemyId in _stunRemainingTime.Keys)
            {
                if (_stunRemainingTime[enemyId] - deltaTime > 0f)
                {
                    continue;
                }

                _expiredStunEnemyIds.Add(enemyId);
            }

            foreach (var enemyId in _expiredStunEnemyIds)
            {
                _stunRemainingTime.Remove(enemyId);
            }

            // 残っている敵の時間を進める（辞書の値の書き換えは列挙後に行う）
            _activeStunEnemyIds.Clear();
            _activeStunEnemyIds.AddRange(_stunRemainingTime.Keys);

            foreach (var enemyId in _activeStunEnemyIds)
            {
                _stunRemainingTime[enemyId] -= deltaTime;
            }

            return _expiredStunEnemyIds;
        }

        public Vector3 GetKnockBackPosition(Vector3 dodgeTargetPosition, Vector3 direction)
        {
            var forward = direction;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0f)
            {
                return dodgeTargetPosition;
            }

            forward.Normalize();

            var knockBackPosition = dodgeTargetPosition + forward * _config.KnockBackDistance;

            // 接触順（1体目は正面、以降は左右交互）にずらして同じ座標へ重ねない。
            // 重ねると押し合いでジッターするため
            var index = ContactedEnemyCount - 1;

            if (index <= 0)
            {
                return knockBackPosition;
            }

            var step = (index + 1) / 2;
            var sign = index % 2 == 1 ? 1f : -1f;
            var right = Vector3.Cross(Vector3.up, forward);

            return knockBackPosition + right * (sign * step * _config.KnockBackSpacing);
        }

        public void Dispose()
        {
            _onEnemyContacted.Dispose();
        }

        /// <summary>
        /// origin を頂点・forward を中心軸とした扇形（半径・左右角度はConfigの設定値）に
        /// targetPosition が入っているかを水平面で判定する。
        /// 壁越しの敵は攻撃対象にしないため、範囲内でも見通しが遮られていれば false を返す。
        /// </summary>
        private bool IsInAttackSector(Vector3 origin, Vector3 forward, Vector3 targetPosition)
        {
            var toTarget = targetPosition - origin;
            toTarget.y = 0f;

            var sqrDistance = toTarget.sqrMagnitude;

            if (sqrDistance > _config.AttackRange * _config.AttackRange)
            {
                return false;
            }

            // 起点と同一座標の敵は角度が定義できないため範囲内として扱う
            if (sqrDistance <= 0f)
            {
                return true;
            }

            if (Vector3.Angle(forward, toTarget.normalized) > _config.AttackHalfAngle)
            {
                return false;
            }

            return !IsSightBlocked(origin, targetPosition);
        }

        /// <summary>
        /// 回避終了地点から対象への見通しが壁（フィールド）に遮られているかを返す。
        /// </summary>
        private bool IsSightBlocked(Vector3 origin, Vector3 targetPosition)
        {
            var start = origin + Vector3.up * _config.SightHeight;
            var end = targetPosition + Vector3.up * _config.SightHeight;

            return Physics.Linecast(start, end, LayerMasks.FieldLayer);
        }
    }
}
