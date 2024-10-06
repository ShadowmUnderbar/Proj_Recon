using System;
using App.Battle.Data;
using App.Battle.Interface;
using UniRx;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Script.Battle.UseCase
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
            _battleHitPresenter.OnHit.Subscribe(OnHit).AddTo(_disposable);
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