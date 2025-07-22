using System;
using App.Battle.Interface;
using App.Battle.Data;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using R3;

namespace App.Battle.Presenters
{
    public class BattleHitPresenter : IBattleHitPresenter
    {
        private readonly IHitBoxStoreView _hitBoxStoreView;

        [Inject]
        public BattleHitPresenter
        (
            IHitBoxStoreView hitBoxStoreView
        )
        {
            _hitBoxStoreView = hitBoxStoreView;
        }

        public Observable<HitData> OnHit => _hitBoxStoreView.OnHitObservable;
    }
}