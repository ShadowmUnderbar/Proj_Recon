using System;
using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    /// <summary>
    /// 敵の撃破でポイント粒子をドロップさせ、回収された粒子ぶんのポイントを加算する。
    /// CACHUMMYYY! を所持していれば、回収した粒子に応じて体力も回復する。
    /// </summary>
    public class PointDropUseCase : IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IPointDataStore _pointDataStore;
        private readonly IPointParticlePresenter _pointParticlePresenter;
        private readonly IPointDropCalculatorDataStore _pointDropCalculator;
        private readonly ICachummyyyDataStore _cachummyyyDataStore;
        private readonly IPlayerStateDataStore _playerStateDataStore;

        private readonly CompositeDisposable _disposable = new();

        // 分割結果の受け皿（撃破のたびに確保しないよう使い回す）
        private readonly List<PointUnitData> _dropUnits = new();

        [Inject]
        public PointDropUseCase(
            IEnemyDataStore enemyDataStore,
            IPointDataStore pointDataStore,
            IPointParticlePresenter pointParticlePresenter,
            IPointDropCalculatorDataStore pointDropCalculator,
            ICachummyyyDataStore cachummyyyDataStore,
            IPlayerStateDataStore playerStateDataStore
        )
        {
            _enemyDataStore = enemyDataStore;
            _pointDataStore = pointDataStore;
            _pointParticlePresenter = pointParticlePresenter;
            _pointDropCalculator = pointDropCalculator;
            _cachummyyyDataStore = cachummyyyDataStore;
            _playerStateDataStore = playerStateDataStore;
        }

        public void Initialize()
        {
            // 撃破演出の完了を待たずにドロップさせる（敵データが消される前に位置を取る必要がある）
            _enemyDataStore.OnEnemyDead
                .Subscribe(OnEnemyDead)
                .AddTo(_disposable);

            _pointParticlePresenter.OnCollected
                .Subscribe(OnCollected)
                .AddTo(_disposable);
        }

        private void OnEnemyDead(int enemyId)
        {
            if (!_enemyDataStore.TryGetEnemyData(enemyId, out var enemyData))
            {
                return;
            }

            if (!_enemyDataStore.TryGetEnemyMasterData(enemyData.EnemyMasterDataId, out var enemyMasterData))
            {
                return;
            }

            _pointDropCalculator.Split(_pointDropCalculator.GetDropPoint(enemyMasterData), _dropUnits);

            if (_dropUnits.Count == 0)
            {
                return;
            }

            _pointParticlePresenter.Spawn(enemyData.Pose.position, _dropUnits);
        }

        private void OnCollected(int point)
        {
            _pointDataStore.Add(point);

            _playerStateDataStore.Heal(_cachummyyyDataStore.GetHealAmount(point));
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
