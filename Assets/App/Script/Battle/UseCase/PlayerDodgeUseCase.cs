using System;
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
    public class PlayerDodgeUseCase : IInitializable, IDisposable
    {
        private readonly IPlayerDataStore _playerDataStore;
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPlayerDodgeParameterDataStore _playerDodgeParameterDataStore;
        private readonly IGameInputUseCase _gameInputUseCase;
        private readonly IEnemyPresenter _enemyPresenter;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public PlayerDodgeUseCase(
            IPlayerDataStore playerDataStore,
            IEnemyDataStore enemyDataStore,
            IPlayerDodgeParameterDataStore playerDodgeParameterDataStore,
            IGameInputUseCase gameInputUseCase,
            IEnemyPresenter enemyPresenter
        )
        {
            _playerDataStore = playerDataStore;
            _enemyDataStore = enemyDataStore;
            _playerDodgeParameterDataStore = playerDodgeParameterDataStore;
            _gameInputUseCase = gameInputUseCase;
            _enemyPresenter = enemyPresenter;
        }


        public void Initialize()
        {
            _gameInputUseCase.IsLeftGrip
                .Where(x => x)
                .Subscribe(_ => OnDodge())
                .AddTo(_disposables);

            _gameInputUseCase.IsRightTrigger
                .Where(x => x)
                .Subscribe(_ => OnDodge())
                .AddTo(_disposables);

            _gameInputUseCase.IsDodge
                .Where(x => x)
                .Subscribe(_ => OnDodge())
                .AddTo(_disposables);
        }

        private void OnDodge()
        {
            if (_gameInputUseCase.V2LeftAxis == Vector2.zero)
            {
                Debug.Log("Dodge : No Direction");
                return;
            }

            if (!_playerDodgeParameterDataStore.CanDodge)
            {
                Debug.Log("Dodge : Cant Dodge_" + _playerDodgeParameterDataStore.DodgeCount.Value);
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

            var moveDistance = Vector3.Distance(moveTarget, playerPosition);
            var enemyHits = _enemyPresenter.GetDodgeHitEnemies(playerPosition, dodgeDirection.normalized, moveDistance);

            if (enemyHits is { Length: > 0 })
            {
                var damage = _playerDodgeParameterDataStore.DodgeDamage;

                foreach (var enemyId in enemyHits)
                {
                    _enemyDataStore.Damage(enemyId, damage);
                }
            }

            Debug.Log("Dodge : " + _playerDataStore.Position.Value + " -> " + moveTarget + " " +
                      moveDistance);
            _playerDataStore.Position.Value = moveTarget;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}