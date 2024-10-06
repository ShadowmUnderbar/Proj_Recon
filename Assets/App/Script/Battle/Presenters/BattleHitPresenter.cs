using System;
using App.Battle.Interface;
using App.Battle.Data;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using R3;

namespace App.Battle.Presenters
{
    public class BattleHitPresenter : IBattleHitPresenter, IInitializable, IDisposable
    {
        private readonly IBattlePlayerView _playerView;

        public Observable<HitData> OnHit => _onHit;
        private readonly Subject<HitData> _onHit = new();

        private readonly CompositeDisposable _disposable = new();

        [Inject]
        public BattleHitPresenter
        (
            IBattlePlayerView playerView
        )
        {
            _playerView = playerView;
        }

        public void Initialize()
        {
            _playerView.OnHit.Subscribe(x => _onHit.OnNext(x)).AddTo(_disposable);
        }

        public void Dispose()
        {
            _onHit?.Dispose();
            _disposable.Dispose();
        }
    }
}