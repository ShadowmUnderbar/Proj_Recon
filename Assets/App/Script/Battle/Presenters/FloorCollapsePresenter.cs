using App.Battle.Data;
using App.Battle.Interface;
using VContainer;

namespace App.Battle.Presenters
{
    public class FloorCollapsePresenter : IFloorCollapsePresenter
    {
        private readonly IFloorCollapseView _floorCollapseView;

        [Inject]
        public FloorCollapsePresenter(IFloorCollapseView floorCollapseView)
        {
            _floorCollapseView = floorCollapseView;
        }

        public void Begin(FloorCollapseWave wave) => _floorCollapseView.Begin(wave);
        public void SetProgress(float progress) => _floorCollapseView.SetProgress(progress);
        public void Restore() => _floorCollapseView.Restore();
    }
}
