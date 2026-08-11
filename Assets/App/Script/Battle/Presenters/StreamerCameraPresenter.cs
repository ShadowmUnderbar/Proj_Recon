using App.Battle.Data;
using App.Battle.Interface;
using R3;
using VContainer;

namespace App.Battle.Presenters
{
    public class StreamerCameraPresenter : IStreamerCameraPresenter
    {
        private readonly IStreamerCameraView _streamerCameraView;

        public Observable<Unit> OnShotFinished => _streamerCameraView.OnShotFinished;

        [Inject]
        public StreamerCameraPresenter(IStreamerCameraView streamerCameraView)
        {
            _streamerCameraView = streamerCameraView;
        }

        public void PlayShot(StreamerCameraShotRequest request) => _streamerCameraView.PlayShot(request);
        public void CancelShot() => _streamerCameraView.CancelShot();
    }
}
