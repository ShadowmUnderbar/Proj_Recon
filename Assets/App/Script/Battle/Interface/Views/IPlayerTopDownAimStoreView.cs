using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimStoreView
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
        void SetRayColor(Color color);
        void Aim();
        void Shot(ShotType shotType, int focusTargetId, bool isLeft);
    }
}