using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IFloorCollapsePresenter
    {
        void Begin(FloorCollapseWave wave);
        void SetProgress(float progress);
        void Restore();
    }
}
