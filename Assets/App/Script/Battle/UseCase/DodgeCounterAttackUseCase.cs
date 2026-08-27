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
    /// 回避中に巻き込んだ敵弾・敵を数え、回避終了時に終了地点から回避方向へ扇形の攻撃を発生させる。
    /// あわせて回避方向へ直線（SphereCast）の判定も出し、扇形と重複しない敵を攻撃する。
    /// 回避中に接触した敵は回避方向へ押し出し、扇形範囲外でも必ず攻撃対象に含める。
    /// </summary>
    public class DodgeCounterAttackUseCase : IInitializable, IDisposable
    {

        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IDodgeCounterAttackDataStore _dodgeCounterAttackDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IPlayerStateDataStore _playerStateDataStore;

        // レイ演出の高さなどの調整値
        private readonly DodgeCounterAttackConfig _config;

        private readonly CompositeDisposable _disposable = new();

        // 押し出す敵の作業リスト（左右へ散らす位置を生存数基準で決めるため、先に絞ってから使う）
        private readonly List<int> _pushTargetEnemyIds = new();

        [Inject]
        public DodgeCounterAttackUseCase(
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IDodgeCounterAttackDataStore dodgeCounterAttackDataStore,
            IEnemyDataStore enemyDataStore,
            IEnemyPresenter enemyPresenter,
            IWaveManagerDataStore waveManagerDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IPlayerStateDataStore playerStateDataStore,
            DodgeCounterAttackConfig config
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
        }

        public void Initialize()
        {
            _playerDodgeParameterDataStore.OnDamagedDuringDodge
                .Subscribe(OnDamageBlocked)
                .AddTo(_disposable);

            _playerDodgeParameterDataStore.OnDodgeEnd
                .Subscribe(OnDodgeEnd)
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

        private void OnDodgeEnd(DodgeEndData dodgeEndData)
        {
            // ウェーブ間ポーズ中は敵へダメージを通さない（他の攻撃と同基準）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                _dodgeCounterAttackDataStore.ResetContacts();
                return;
            }

            var origin = dodgeEndData.EndPosition;
            var direction = dodgeEndData.Direction;

            var damage = _dodgeCounterAttackDataStore.CalcDamage();
            var tracerWidth = _dodgeCounterAttackDataStore.GetTracerWidth();
            var targetEnemyIds = _dodgeCounterAttackDataStore.GetTargetEnemyIds(origin, direction);
            var tracerHeight = Vector3.up * _config.TracerHeight;

            // ダメージは押し出し前の座標で確定させる（押し出し直後は敵の座標がまだ更新されておらず、
            // 弱点方向の判定と傾き演出の向きが押し出し前後で食い違うため）
            for (var i = 0; i < targetEnemyIds.Count; i++)
            {
                if (!Damage(targetEnemyIds[i], origin, damage, out var enemyPosition))
                {
                    continue;
                }

                // 扇形の対象へは1体ずつレイ演出を出す
                PlayTracer(origin + tracerHeight, enemyPosition + tracerHeight, tracerWidth);
            }

            // 直線判定（対象の有無に関わらずレイ演出を出す）
            LineAttack(origin, direction, damage, tracerWidth);

            // 巻き込んだ敵は回避方向へ押し出す
            PushContactedEnemies(origin, direction);

            // カウントは回避終了時点で確定。次の回避に備えてリセットする
            _dodgeCounterAttackDataStore.ResetContacts();
        }

        /// <summary>
        /// 回避方向へ直線（SphereCast）の判定を出し、扇形範囲と重複しない敵を攻撃する。
        /// レイ演出は対象がいなくても必ず再生する。
        /// </summary>
        private void LineAttack(Vector3 origin, Vector3 direction, float damage, float tracerWidth)
        {
            var radius = _dodgeCounterAttackDataStore.GetLineAttackRadius();
            var distance = GetLineDistance(origin, direction);

            // 判定は足元ではなく胴体あたりの高さから飛ばす
            var castOrigin = origin + Vector3.up * _config.SightHeight;

            var lineHitEnemyIds = _enemyPresenter.GetLineHitEnemies(castOrigin, direction, radius, distance);
            var lineTargetEnemyIds = _dodgeCounterAttackDataStore.GetLineTargetEnemyIds(lineHitEnemyIds);

            for (var i = 0; i < lineTargetEnemyIds.Count; i++)
            {
                Damage(lineTargetEnemyIds[i], origin, damage, out _);
            }

            // 直線のレイ演出は1本だけ、当たった敵の有無に関わらず出す
            var tracerStart = origin + Vector3.up * _config.TracerHeight;
            PlayTracer(tracerStart, tracerStart + direction * distance, tracerWidth);
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

        private void PushContactedEnemies(Vector3 origin, Vector3 direction)
        {
            var contactedEnemyIds = _dodgeCounterAttackDataStore.ContactedEnemyIds;

            // 左右へ散らす位置は「実際に押し出す敵の数」を基準にしたいので、先に対象を絞る
            _pushTargetEnemyIds.Clear();

            for (var i = 0; i < contactedEnemyIds.Count; i++)
            {
                var enemyId = contactedEnemyIds[i];

                // 撃破演出中の敵は動かさない
                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData) || enemyData.IsDead)
                {
                    continue;
                }

                _pushTargetEnemyIds.Add(enemyId);
            }

            for (var i = 0; i < _pushTargetEnemyIds.Count; i++)
            {
                var pushPosition = _dodgeCounterAttackDataStore.GetPushPosition(
                    origin, direction, i, _pushTargetEnemyIds.Count);

                _enemyPresenter.Push(_pushTargetEnemyIds[i], pushPosition);
            }
        }

        /// <summary>
        /// 対象へダメージと被弾演出を与える。与えられた場合のみ true を返し、
        /// レイ演出の着弾地点として敵の座標を out で返す。
        /// </summary>
        private bool Damage(int enemyId, Vector3 origin, float damage, out Vector3 enemyPosition)
        {
            enemyPosition = origin;

            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return false;
            }

            // 撃破演出中の敵はダメージも演出も通さない（EnemyDataStore.Damage と同基準）
            if (enemyData.IsDead)
            {
                return false;
            }

            enemyPosition = enemyData.Pose.position;

            var directionType = RelativeYawExtension.GetActorRelative(enemyData.Pose, origin);

            // 跳ね返しの水平方向（回避終了地点→敵）。傾き演出の向きに使う
            var hitDirection = enemyData.Pose.position - origin;
            hitDirection.y = 0f;
            hitDirection = hitDirection.sqrMagnitude > 0f ? hitDirection.normalized : Vector3.zero;

            // ShotTypeは渡さない（射撃そのものではないため、フォーム条件のバフを駆動させない）
            _enemyDataStore.Damage(new HitData(enemyId, damage, directionType, hitDirection));

            // 弾のヒットボックスを経由しないため、被弾の傾き演出は明示的に再生する
            _enemyPresenter.PlayHitFeedback(enemyId, hitDirection);

            return true;
        }

        /// <summary>
        /// ノーマル弾の即着弾と同じレイ演出を出す（座標は呼び出し側で高さを含めて渡す）。
        /// </summary>
        private void PlayTracer(Vector3 from, Vector3 to, float tracerWidth)
        {
            _playerControlPresenter.PlayShotTracer(from, to, tracerWidth);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
