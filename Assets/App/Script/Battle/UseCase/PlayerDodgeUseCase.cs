using System;
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
    public class PlayerDodgeUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerStateDataStore _playerStateDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IGameInputDataStore _gameInputUseCase;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;
        private readonly IPlayerControlPresenter _playerControlPresenter;
        private readonly IWaveManagerDataStore _waveManagerDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerDodgeUseCase(
            IPlayerStateDataStore playerStateDataStore,
            IEnemyDataStore enemyDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IGameInputDataStore gameInputUseCase,
            IEnemyPresenter enemyPresenter,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore,
            IPlayerControlPresenter playerControlPresenter,
            IWaveManagerDataStore waveManagerDataStore
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

            Blitz(playerPosition, dodgeDirection, moveTarget);

            _playerControlPresenter.Blitz(_playerStateDataStore.Position.Value, _playerStateDataStore.PlayerTransform);
            _playerStateDataStore.Position.Value = moveTarget;
        }

        private void Blitz(Vector3 playerPosition, Vector3 dodgeDirection, Vector3 moveTarget)
        {
            if (!_coreSkillUnlockDataStore.IsUnLockBlitz)
            {
                return;
            }

            var beforePosition = _playerStateDataStore.Position.Value;
            var moveDistance = Vector3.Distance(moveTarget, playerPosition);
            var enemyHits = _enemyPresenter.GetDodgeHitEnemies(playerPosition, dodgeDirection.normalized, moveDistance);

            if (enemyHits is null or { Length: <= 0 })
            {
                return;
            }

            var damage = _playerDodgeParameterDataStore.DodgeDamage;

            foreach (var enemyId in enemyHits)
            {
                if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
                {
                    continue;
                }

                var directionType = RelativeYawExtension.GetActorRelative(enemyData.Pose, beforePosition);
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