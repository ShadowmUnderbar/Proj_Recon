using App.Battle.Data;
using App.Battle.Interface;
using VContainer;

namespace App.Battle.Presenters
{
    public class AfterimagePresenter : IAfterimagePresenter
    {
        private readonly IBattlePlayerView _playerView;
        private readonly IAfterimageStoreView _afterimageStoreView;

        [Inject]
        public AfterimagePresenter(IBattlePlayerView playerView, IAfterimageStoreView afterimageStoreView)
        {
            _playerView = playerView;
            _afterimageStoreView = afterimageStoreView;
        }

        public void SpawnPlayerAfterimage(AfterimageStyle style)
        {
            var source = _playerView.AfterimageSource;
            if (source == null)
            {
                return;
            }

            _afterimageStoreView.Spawn(source, style);
        }
    }
}
