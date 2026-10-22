using App.Battle.Data;
using App.Battle.Interface;
using UnityEngine;
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

        public void Begin(Vector3 origin, FloorCollapseSettings settings) => _floorCollapseView.Begin(origin, settings);
        public void SetProgress(float progress) => _floorCollapseView.SetProgress(progress);
        public void Restore() => _floorCollapseView.Restore();
    }
}
