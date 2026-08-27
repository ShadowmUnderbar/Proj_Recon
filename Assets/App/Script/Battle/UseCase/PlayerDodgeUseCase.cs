using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
using App.Framework.Utilities.Extensions;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 回避入力を受けて、プレイヤーを一定距離だけ直線で素早く移動させる。
    /// 移動中は <see cref="IPlayerDodgeParameterDataStore.IsDodging"/> が true になり、
    /// 被弾は <see cref="PlayerHitUseCase"/> 側で無効化される。
    /// </summary>
    public class PlayerDodgeUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IGameInputDataStore _gameInputUseCase;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IDodgeCounterAttackDataStore _dodgeCounterAttackDataStore;

        private readonly CompositeDisposable _disposables = new();

        // 1回の回避中にBlitzのダメージを与えた敵ID（同じ敵への多重ヒットを防ぐ）
        private readonly HashSet<int> _blitzHitEnemyIds = new();

        [Inject]
        public PlayerDodgeUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IEnemyDataStore enemyDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IGameInputDataStore gameInputUseCase,
            IEnemyPresenter enemyPresenter,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IWaveManagerDataStore waveManagerDataStore,
            IDodgeCounterAttackDataStore dodgeCounterAttackDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _enemyDataStore = enemyDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _gameInputUseCase = gameInputUseCase;
            _enemyPresenter = enemyPresenter;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
            _playerControlPresenter = playerControlPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _dodgeCounterAttackDataStore = dodgeCounterAttackDataStore;
        }


        public void Initialize()
        {
            _gameInputUseCase.IsDodge
                .Where(x => x)
                .Subscribe(_ => OnDodge())
                .AddTo(_disposables);
        }

        private void OnDodge()
        {
            // ウェーブ間ポーズ中は回避を停止（Blitzの直接ダメージも防ぐ）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            if (_gameInputUseCase.V2LeftAxis == Vector2.zero)
            {
                return;
            }

            // 回避移動中の再入力は無視する（移動が上書きされて距離が狂うのを防ぐ）
            if (_playerDodgeParameterDataStore.IsDodging.CurrentValue)
            {
                return;
            }

            if (!_playerDodgeParameterDataStore.CanDodge)
            {
                return;
            }

            _playerDodgeParameterDataStore.SetCoolDownTime();

            var playerPosition = _playerStateDataStore.Position.Value;
            var dodgeDirection = new Vector3(_gameInputUseCase.V2LeftAxis.x, 0, _gameInputUseCase.V2LeftAxis.y);
            var moveTarget = playerPosition +
                             dodgeDirection * _playerDodgeParameterDataStore.DodgeRange;

            var ray = new Ray(playerPosition + Vector3.up,
                dodgeDirection.normalized);

            if (Physics.Raycast(ray, out var hit, _playerDodgeParameterDataStore.DodgeRange, LayerMasks.FieldLayer))
            {
                moveTarget = hit.point;
            }

            _playerControlPresenter.Blitz(_playerStateDataStore.Position.Value, _playerStateDataStore.PlayerTransform);

            // 瞬間移動ではなく、Tickで一定時間かけて直線移動させる
            // Blitzのダメージも移動に合わせてTickで順次与える
            _blitzHitEnemyIds.Clear();

            // 前回の回避の接触記録が残らないよう、回避開始時にもリセットする
            _dodgeCounterAttackDataStore.ResetContacts();

            _playerDodgeParameterDataStore.StartDodge(playerPosition, moveTarget);
        }

        public void Tick()
        {
            // ウェーブ間ポーズ中は回避移動も止める（再開時に残り距離を移動する）
            if (_waveManagerDataStore.IsWavePause.Value)
            {
                return;
            }

            var previousPosition = _playerStateDataStore.Position.Value;

            if (!_playerDodgeParameterDataStore.TryAdvanceDodge(Time.deltaTime, out var position,
                    out var isFinished))
            {
                return;
            }

            _playerStateDataStore.Position.Value = position;

            // このフレームで通過した区間だけを判定し、すり抜けた敵を拾う
            var enemyHits = GetDodgeHitEnemies(previousPosition, position);

            // 回避時跳ね返し攻撃の接触記録（Blitzの解放状況に関係なく毎回行う）
            RegisterEnemyContacts(enemyHits);

            // すり抜けた敵に順番にダメージを与える
            Blitz(enemyHits, previousPosition);

            if (!isFinished)
            {
                return;
            }

            // 到達フレームの接触記録まで済んだ後に回避終了を通知する（跳ね返し攻撃の発生点）
            _playerDodgeParameterDataStore.NotifyDodgeEnd();
        }

        /// <summary>
        /// 回避で通過した区間 from→to にいる敵のIdを返す（いなければ空）。
        /// 戻り値はPresenter側の使い回しリストなので、次の呼び出しまでに使い切る。
        /// </summary>
        private IReadOnlyList<int> GetDodgeHitEnemies(Vector3 from, Vector3 to)
        {
            var moveVector = to - from;
            var moveDistance = moveVector.magnitude;

            if (moveDistance <= 0f)
            {
                return Array.Empty<int>();
            }

            return _enemyPresenter.GetDodgeHitEnemies(from, moveVector.normalized, moveDistance);
        }

        /// <summary>
        /// 回避で接触した敵を跳ね返し攻撃へ記録する（重複除外はDataStore側が行う）。
        /// </summary>
        private void RegisterEnemyContacts(IReadOnlyList<int> enemyHits)
        {
            for (var i = 0; i < enemyHits.Count; i++)
            {
                _dodgeCounterAttackDataStore.RegisterEnemyContact(enemyHits[i]);
            }
        }

        /// <summary>
        /// 回避で通過した区間にいた敵へダメージを与える。
        /// 同一の回避中に同じ敵へ複数回ダメージが入らないよう、命中済みIDを保持する。
        /// </summary>
        private void Blitz(IReadOnlyList<int> enemyHits, Vector3 from)
        {
            if (!_coreSkillUnlockDataStore.IsUnLockBlitz)
            {
                return;
            }

            var damage = _playerDodgeParameterDataStore.DodgeDamage;

            for (var i = 0; i < enemyHits.Count; i++)
            {
                var enemyId = enemyHits[i];

                if (!_blitzHitEnemyIds.Add(enemyId))
                {
                    continue;
                }

                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
                {
                    continue;
                }

                var directionType = RelativeYawExtension.GetActorRelative(enemyData.Pose, from);
                var hitData = new HitData(enemyId, damage, directionType);

                _enemyDataStore.Damage(hitData);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}