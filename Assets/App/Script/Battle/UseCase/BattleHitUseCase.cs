using System;
using App.Battle.Data;
using App.Battle.Interface;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Battle.UseCase
{
    public class BattleHitUseCase : IBattleHitUseCase, IInitializable, IDisposable
    {
        private readonly IBattleHitPresenter _battleHitPresenter;

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BattleHitUseCase
        (
            IBattleHitPresenter battleHitPresenter
        )
        {
            _battleHitPresenter = battleHitPresenter;
        }

        public void Initialize()
        {
            _battleHitPresenter.OnHit.Subscribe(x => OnHit(x)).AddTo(_disposable);
        }

        private void OnHit(HitData hitData)
        {
            Debug.Log("ヒットァ！！！！");
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}