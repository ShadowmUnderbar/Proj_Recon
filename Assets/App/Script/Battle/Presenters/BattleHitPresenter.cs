using System;
using App.Battle.Interface.Presenters;
using App.Battle.Interface.Views;
using App.Script.Battle.Data;
using Cysharp.Threading.Tasks;
using UniRx;
using VContainer;
using VContainer.Unity;

namespace App.Battle.Presenters
{
    public class BattleHitPresenter : IBattleHitPresenter, IInitializable, IDisposable
    {
        private readonly IBattlePlayerView _playerView;

        public IObservable<HitData> OnHit => _onHit;
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
            _playerView.OnHit.Subscribe(_onHit).AddTo(_disposable);
        }

        public void Dispose()
        {
            _onHit?.Dispose();
            _disposable.Dispose();
        }
    }
}