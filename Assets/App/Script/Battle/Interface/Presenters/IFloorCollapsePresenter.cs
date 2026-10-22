using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IFloorCollapsePresenter
    {
        void Begin(Vector3 origin, FloorCollapseSettings settings);
        void SetProgress(float progress);
        void Restore();
    }
}
