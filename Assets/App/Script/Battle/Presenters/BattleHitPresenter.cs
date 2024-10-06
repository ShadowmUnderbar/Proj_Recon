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
        private readonly IBattlePlayerView _playerView;

        public Observable<HitData> OnHit => _playerView.OnHit;


        [Inject]
        public BattleHitPresenter
        (
            IBattlePlayerView playerView
        )
        {
            _playerView = playerView;
        }
    }
}