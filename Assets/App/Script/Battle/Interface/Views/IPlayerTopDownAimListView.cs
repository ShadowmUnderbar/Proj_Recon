using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimListView
    {
        void InitStoreView(
            IPlayerTopDownAimView leftTopDown, IPlayerTopDownAimView rightTopDown,
            IPlayerAimMuzzleView leftAim, IPlayerAimMuzzleView rightAim,
            IPlayerShotView leftShot, IPlayerShotView rightShot);

        Observable<HitData> OnHit { get; }

        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }

        Observable<Vector3> OnRightAimPosition { get; }
        Observable<Vector3> OnLeftAimPosition { get; }
        void SetRayColor(HandType handType, Color color);
        void SetEnableRay(HandType handType, bool enable);
        void Aim();
        void Shot(HandType handType, ShotType shotType, AimFocusType focusType, int focusTargetId);
    }
}