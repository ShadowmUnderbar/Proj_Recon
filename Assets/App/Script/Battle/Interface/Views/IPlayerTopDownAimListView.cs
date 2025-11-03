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

        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }

        Observable<Vector3> OnRightAimPosition { get; }
        Observable<Vector3> OnLeftAimPosition { get; }

        void IsFocusLeft(bool isFocus);
        void IsFocusRight(bool isFocus);
        void SetRayColor(HandType handType, Color color);
        void SetEnableRay(HandType handType, bool enable);
        void Aim();
        void Shot(HandType handType, BulletData bulletData, int focusTargetId);
    }
}