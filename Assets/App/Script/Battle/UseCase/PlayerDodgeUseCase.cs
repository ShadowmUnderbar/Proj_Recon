using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using App.Common.Interface;
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
    /// 通過した敵は接触として記録し、スタン・吹き飛ばし・ダメージは
    /// <see cref="DodgeCounterAttackUseCase"/> 側で扱う。
    /// </summary>
    public class PlayerDodgeUseCase : IInitializable, ITickable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IGameInputDataStore _gameInputUseCase;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;
        private readonly IDodgeCounterAttackDataStore _dodgeCounterAttackDataStore;
        private readonly IFreezeDataStore _freezeDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerDodgeUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IGameInputDataStore gameInputUseCase,
            IEnemyPresenter enemyPresenter,
            IPlayerControlPresenter playerControlPresenter,
            IWaveManagerDataStore waveManagerDataStore,
            IDodgeCounterAttackDataStore dodgeCounterAttackDataStore,
            IFreezeDataStore freezeDataStore
        )
        {
            _playerStateDataStore = playerStateDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _gameInputUseCase = gameInputUseCase;
            _enemyPresenter = enemyPresenter;
            _playerControlPresenter = playerControlPresenter;
            _waveManagerDataStore = waveManagerDataStore;
            _dodgeCounterAttackDataStore = dodgeCounterAttackDataStore;
            _freezeDataStore = freezeDataStore;
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
            // ウェーブ間ポーズ中・フリーズ中は回避を停止（Blitzの直接ダメージも防ぐ）
            if (_waveManagerDataStore.IsWavePause.Value ||
                _freezeDataStore.IsFreezing.CurrentValue)
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
            // 通過した敵の接触判定も移動に合わせてTickで順次行う

            // 前回の回避の接触記録が残らないよう、回避開始時にもリセットする
            _dodgeCounterAttackDataStore.ResetContacts();

            _playerDodgeParameterDataStore.StartDodge(playerPosition, moveTarget);
        }

        public void Tick()
        {
            // ウェーブ間ポーズ中・フリーズ中は回避移動も止める（再開時に残り距離を移動する）
            if (_waveManagerDataStore.IsWavePause.Value ||
                _freezeDataStore.IsFreezing.CurrentValue)
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

            // このフレームで通過した区間だけを判定し、すり抜けた敵を拾う。
            // 接触した敵はその時点でスタン＋吹き飛ばしが走り、ダメージは回避終了時にまとめて入る
            RegisterEnemyContacts(GetDodgeHitEnemies(previousPosition, position));

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

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}