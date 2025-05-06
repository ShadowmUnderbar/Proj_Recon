using System;
using App.Battle.Data;
using App.Battle.Interface;
using App.Battle.Interface.DataStore;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class BattleHitUseCase : IBattleHitUseCase, IInitializable, IDisposable
    {
        private readonly IEnemyDataStore _enemyDataStore;
        private readonly IBattleHitPresenter _battleHitPresenter;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BattleHitUseCase
        (
            IEnemyDataStore enemyDataStore,
            IBattleHitPresenter battleHitPresenter
        )
        {
            _enemyDataStore = enemyDataStore;
            _battleHitPresenter = battleHitPresenter;
        }

        public void Initialize()
        {
            _battleHitPresenter.OnHit.Subscribe(x => OnHit(x)).AddTo(_disposable);
        }

        private void OnHit(HitData hitData)
        {
            _enemyDataStore.Damage(hitData.DamagedId, hitData.Damage);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}