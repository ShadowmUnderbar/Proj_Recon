using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IStreamerCameraPresenter
    {
        Observable<Unit> OnShotFinished { get; }

        void PlayShot(StreamerCameraShotRequest request);
        void CancelShot();
    }
}
