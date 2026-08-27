using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Framework.Utilities.Extensions;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 回避時跳ね返し攻撃。
    /// 回避中に接触した敵はその瞬間にスタンし、回避先＋一定距離の地点へイージングで吹き飛ばす。
    /// 敵弾・敵を1つ以上巻き込んでいた場合のみ、回避終了時に
    /// 「攻撃対象の検索（扇形＋直線）→ レイ演出 → フリーズ → ダメージ」を行う。
    /// 接触した敵は扇形範囲外でも必ず攻撃対象に含める。
    /// スタンは「吹き飛ばしの移動時間＋フリーズ時間」で自動的に解除される。
    /// </summary>
    public class DodgeCounterAttackUseCase : IInitializable, ITickable, IDisposable
    {

        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IDodgeCounterAttackDataStore _dodgeCounterAttackDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IPlayerStateDataStore _playerStateDataStore;

        private readonly IFreezeDataStore _freezeDataStore;

        // レイ演出の高さなどの調整値
        private readonly DodgeCounterAttackConfig _config;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public DodgeCounterAttackUseCase(
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IDodgeCounterAttackDataStore dodgeCounterAttackDataStore,
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IWaveManagerDataStore waveManagerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IPlayerStateDataStore playerStateDataStore,
            DodgeCounterAttackConfig config,
            IFreezeDataStore freezeDataStore
        )
        {
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _dodgeCounterAttackDataStore = dodgeCounterAttackDataStore;
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _playerStateDataStore = playerStateDataStore;
            _config = config;
            _freezeDataStore = freezeDataStore;
        }

        public void Initialize()
        {
            _playerDodgeParameterDataStore.OnDamagedDuringDodge
                .Subscribe(OnDamageBlocked)
                .AddTo(_disposable);

            _playerDodgeParameterDataStore.OnDodgeEnd
                .Subscribe(OnDodgeEnd)
                .AddTo(_disposable);

            _dodgeCounterAttackDataStore.OnEnemyContacted
                .Subscribe(OnEnemyContacted)
                .AddTo(_disposable);
        }

        /// <summary>
        /// 回避中に無効化した攻撃を接触として記録する。
        /// 弾は弾Idで、近接攻撃は攻撃してきた敵のIdで重複を除外する。
        /// </summary>
        private void OnDamageBlocked(PlayerDamagedData damagedData)
        {
            if (damagedData.IsProjectile)
            {
                _dodgeCounterAttackDataStore.RegisterProjectileContact(damagedData.ProjectileId);
                return;
            }

            // 弾以外は近接攻撃と爆風の両方が流れてくる。爆風は遠方の敵からでも届くため、
            // 近接の間合いにいる敵だけを接触として扱う（遠くの敵が押し出されるのを防ぐ）
            if (!_dodgeCounterAttackDataStore.IsWithinContactRange(
                    damagedData.AttackerId, _playerStateDataStore.Position.Value))
            {
                return;
            }

            _dodgeCounterAttackDataStore.RegisterEnemyContact(damagedData.AttackerId);
        }

        public void Tick()
        {
            // 接触スタンの残り時間を進め、切れた敵から解除する
            var expiredEnemyIds = _dodgeCounterAttackDataStore.UpdateStunTimers(Time.deltaTime);

            for (var i = 0; i < expiredEnemyIds.Count; i++)
            {
                _enemyPresenter.SetStun(expiredEnemyIds[i], false);
            }
        }

        /// <summary>
        /// 回避中に接触した敵をスタンさせ、回避先＋一定距離の地点へ吹き飛ばす。
        /// ダメージは回避終了時にまとめて与えるため、ここでは与えない。
        /// </summary>
        private void OnEnemyContacted(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData) || enemyData.IsDead)
            {
                return;
            }

            // 回避の終了に合わせて到着させるため、移動時間は接触時点の回避の残り時間にする
            var knockBackDuration = _playerDodgeParameterDataStore.RemainingDodgeTime;

            _enemyPresenter.SetStun(enemyId, true);
            _dodgeCounterAttackDataStore.RegisterStun(
                enemyId, _dodgeCounterAttackDataStore.GetContactStunDuration(knockBackDuration));

            var destination = _dodgeCounterAttackDataStore.GetKnockBackPosition(
                _playerDodgeParameterDataStore.DodgeTargetPosition,
                _playerDodgeParameterDataStore.DodgeDirection);

            _enemyPresenter.KnockBack(enemyId, destination, knockBackDuration);
        }

        private void OnDodgeEnd(DodgeEndData dodgeEndData)
        {
            // ウェーブ間ポーズ中は敵へダメージを通さない（他の攻撃と同基準）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                _dodgeCounterAttackDataStore.ResetContacts();
                return;
            }

            // 敵弾・敵を1つも巻き込んでいない回避では、扇形・直線とも攻撃を発生させない
            // （レイ演出も出さない）
            if (!_dodgeCounterAttackDataStore.HasContact)
            {
                _dodgeCounterAttackDataStore.ResetContacts();
                return;
            }

            // プレイヤーの回避先への移動は PlayerDodgeUseCase 側で確定済み。
            // 接触した敵は接触時点でスタン＋吹き飛ばし済みなので、
            // 以降は「攻撃対象の検索 → レイ演出 → フリーズ → ダメージ」の順で処理する
            var origin = dodgeEndData.EndPosition;
            var direction = dodgeEndData.Direction;

            var damage = _dodgeCounterAttackDataStore.CalcDamage();
            var tracerWidth = _dodgeCounterAttackDataStore.GetTracerWidth();
            var tracerHeight = Vector3.up * _config.TracerHeight;

            // 1. 攻撃対象の検索（扇形＋直線。直線は扇形と重複しない敵のみ）
            var targetEnemyIds = _dodgeCounterAttackDataStore.GetTargetEnemyIds(origin, direction);
            var lineDistance = GetLineDistance(origin, direction);
            var lineTargetEnemyIds = SearchLineTargets(origin, direction, lineDistance);

            // 2. レイ演出（扇形の対象へは1体ずつ、直線は対象の有無に関わらず1本）
            for (var i = 0; i < targetEnemyIds.Count; i++)
            {
                if (!_enemyDataStore.TryGetEnemyData(targetEnemyIds[i], out var enemyData))
                {
                    continue;
                }

                PlayTracer(origin + tracerHeight, enemyData.Pose.position + tracerHeight, tracerWidth);
            }

            var lineTracerStart = origin + tracerHeight;
            PlayTracer(lineTracerStart, lineTracerStart + direction * lineDistance, tracerWidth);

            // 3. 敵・プレイヤー・弾をその場で止める（ヒットストップ）
            _freezeDataStore.Freeze(_config.FreezeDuration);

            // 4. ダメージ処理
            for (var i = 0; i < targetEnemyIds.Count; i++)
            {
                Damage(targetEnemyIds[i], origin, damage);
            }

            for (var i = 0; i < lineTargetEnemyIds.Count; i++)
            {
                Damage(lineTargetEnemyIds[i], origin, damage);
            }

            // カウントは回避終了時点で確定。次の回避に備えてリセットする
            _dodgeCounterAttackDataStore.ResetContacts();
        }

        /// <summary>
        /// 回避方向への直線（SphereCast）で、扇形範囲と重複しない攻撃対象を検索する。
        /// </summary>
        private IReadOnlyList<int> SearchLineTargets(Vector3 origin, Vector3 direction, float distance)
        {
            var radius = _dodgeCounterAttackDataStore.GetLineAttackRadius();

            // 判定は足元ではなく胴体あたりの高さから飛ばす
            var castOrigin = origin + Vector3.up * _config.SightHeight;

            var lineHitEnemyIds = _enemyPresenter.GetLineHitEnemies(castOrigin, direction, radius, distance);

            return _dodgeCounterAttackDataStore.GetLineTargetEnemyIds(lineHitEnemyIds);
        }

        /// <summary>
        /// 直線判定の距離を返す。壁（フィールド）に遮られる場合はその地点までに短縮する。
        /// </summary>
        private float GetLineDistance(Vector3 origin, Vector3 direction)
        {
            var maxDistance = _dodgeCounterAttackDataStore.LineAttackDistance;

            // 球で判定すると床（フィールド）を拾ってしまうため、壁までの距離は細いレイで測る
            var ray = new Ray(origin + Vector3.up * _config.SightHeight, direction);

            if (!Physics.Raycast(ray, out var hit, maxDistance, LayerMasks.FieldLayer))
            {
                return maxDistance;
            }

            return hit.distance;
        }

        /// <summary>
        /// 対象へダメージと被弾演出を与える。
        /// </summary>
        private void Damage(int enemyId, Vector3 origin, float damage)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            // 撃破演出中の敵はダメージも演出も通さない（EnemyDataStore.Damage と同基準）
            if (enemyData.IsDead)
            {
                return;
            }

            var directionType = RelativeYawExtension.GetActorRelative(enemyData.Pose, origin);

            // 跳ね返しの水平方向（回避終了地点→敵）。傾き演出の向きに使う
            var hitDirection = enemyData.Pose.position - origin;
            hitDirection.y = 0f;
            hitDirection = hitDirection.sqrMagnitude > 0f ? hitDirection.normalized : Vector3.zero;

            // ShotTypeは渡さない（射撃そのものではないため、フォーム条件のバフを駆動させない）
            _enemyDataStore.Damage(new HitData(enemyId, damage, directionType, hitDirection));

            // 弾のヒットボックスを経由しないため、被弾の傾き演出は明示的に再生する
            _enemyPresenter.PlayHitFeedback(enemyId, hitDirection);
        }

        /// <summary>
        /// カウンター専用のレイ演出を出す（座標は呼び出し側で高さを含めて渡す）。
        /// </summary>
        private void PlayTracer(Vector3 from, Vector3 to, float tracerWidth)
        {
            _playerControlPresenter.PlayCounterTracer(from, to, tracerWidth);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
