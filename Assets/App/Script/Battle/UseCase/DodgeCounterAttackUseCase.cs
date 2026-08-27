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
    /// 回避中に接触した敵は回避方向へ押し出し、扇形範囲外でも必ず攻撃対象に含める。
    /// </summary>
    public class DodgeCounterAttackUseCase : IInitializable, IDisposable
    {
        // レイ演出の高さ（回避終了地点・敵Poseはいずれも足元基準のため胴体あたりを結ぶ）
        private const float TracerHeight = 1f;


        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IDodgeCounterAttackDataStore _dodgeCounterAttackDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IPlayerStateDataStore _playerStateDataStore;

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
            IPlayerStateDataStore playerStateDataStore
        )
        {
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _dodgeCounterAttackDataStore = dodgeCounterAttackDataStore;
            _enemyDataStore = enemyDataStore;
            _enemyPresenter = enemyPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _playerControlPresenter = playerControlPresenter;
            _playerStateDataStore = playerStateDataStore;
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

            // ダメージは押し出し前の座標で確定させる（押し出し直後は敵の座標がまだ更新されておらず、
            // 弱点方向の判定と傾き演出の向きが押し出し前後で食い違うため）
            for (var i = 0; i < targetEnemyIds.Count; i++)
            {
                Damage(targetEnemyIds[i], origin, damage, tracerWidth);
            }

            // 巻き込んだ敵は回避方向へ押し出す
            PushContactedEnemies(origin, direction);

            // カウントは回避終了時点で確定。次の回避に備えてリセットする
            _dodgeCounterAttackDataStore.ResetContacts();
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

        private void Damage(int enemyId, Vector3 origin, float damage, float tracerWidth)
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

            // 回避終了地点から対象へ、ノーマル弾の即着弾と同じレイ演出を出す
            _playerControlPresenter.PlayShotTracer(
                origin + Vector3.up * TracerHeight,
                enemyData.Pose.position + Vector3.up * TracerHeight,
                tracerWidth);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
