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
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IGameInputDataStore _gameInputUseCase;
        private readonly IEnemyPresenter _enemyPresenter;
        private readonly ICoreSkillUnlockDataStore _coreSkillUnlockDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerDodgeUseCase(
            IPlayerDataStore playerDataStore,
            IEnemyDataStore enemyDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IGameInputDataStore gameInputUseCase,
            IEnemyPresenter enemyPresenter,
            ICoreSkillUnlockDataStore coreSkillUnlockDataStore
        )
        {
            _playerDataStore = playerDataStore;
            _enemyDataStore = enemyDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _gameInputUseCase = gameInputUseCase;
            _enemyPresenter = enemyPresenter;
            _coreSkillUnlockDataStore = coreSkillUnlockDataStore;
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
            if (_gameInputUseCase.V2LeftAxis == Vector2.zero)
            {
                return;
            }

            if (!_playerDodgeParameterDataStore.CanDodge)
            {
                return;
            }

            _playerDodgeParameterDataStore.SetCoolDownTime();

            var playerPosition = _playerDataStore.Position.Value;
            var dodgeDirection = new Vector3(_gameInputUseCase.V2LeftAxis.x, 0, _gameInputUseCase.V2LeftAxis.y);
            var moveTarget = playerPosition +
                             dodgeDirection * _playerDodgeParameterDataStore.DodgeRange;

            var ray = new Ray(playerPosition + Vector3.up,
                dodgeDirection.normalized);

            if (Physics.Raycast(ray, out var hit, _playerDodgeParameterDataStore.DodgeRange, LayerMasks.FieldLayer))
            {
                moveTarget = hit.point;
            }

            Catalyst(playerPosition, dodgeDirection, moveTarget);

            _playerDataStore.Position.Value = moveTarget;
        }

        private void Catalyst(Vector3 playerPosition, Vector3 dodgeDirection, Vector3 moveTarget)
        {
            if (!_coreSkillUnlockDataStore.IsUnLockCatalyst)
            {
                return;
            }

            var beforePosition = _playerDataStore.Position.Value;
            var moveDistance = Vector3.Distance(moveTarget, playerPosition);
            var enemyHits = _enemyPresenter.GetDodgeHitEnemies(playerPosition, dodgeDirection.normalized, moveDistance);

            if (enemyHits is { Length: <= 0 })
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