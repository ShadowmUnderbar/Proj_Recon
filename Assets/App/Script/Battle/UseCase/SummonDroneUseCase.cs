using System;
using App.Battle.Data;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// サモンドローン: 敵を一定数撃破するたびに、プレイヤーの位置へ味方ドローンを召喚する。
    /// 撃破は誰によるもの（プレイヤー・味方）でも数える
    /// </summary>
    public class SummonDroneUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly ISummonDroneDataStore _summonDroneDataStore;
        private readonly IAllyDataStore _allyDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public SummonDroneUseCase(
            IEnemyDataStore enemyDataStore,
            ISummonDroneDataStore summonDroneDataStore,
            IAllyDataStore allyDataStore,
            IPlayerStateDataStore playerStateDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
            _summonDroneDataStore = summonDroneDataStore;
            _allyDataStore = allyDataStore;
            _playerStateDataStore = playerStateDataStore;
        }

        public void Initialize()
        {
            _enemyDataStore.OnEnemyDead
                .Subscribe(_ => OnEnemyDead())
                .AddTo(_disposables);
        }

        private void OnEnemyDead()
        {
            var count = _summonDroneDataStore.NotifyKill();

            for (var i = 0; i < count; i++)
            {
                // 向きはドローン自身が追従で決めるので指定しない
                _allyDataStore.TrySpawn(AllyType.Drone, _playerStateDataStore.Position.Value, Vector3.zero, out _);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
