using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerControlPresenter
    {
        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }
        Observable<Vector3> OnLeftAimPosition { get; }
        Observable<Vector3> OnRightAimPosition { get; }
        ReactiveProperty<Vector3> OnUpdatePosition { get; }
        ReactiveProperty<Pose> LeftHandPose { get; }
        ReactiveProperty<Pose> RightHandPose { get; }

        void Move(Vector2 moveV2);
        void Aim();
        void SetHandRayColor(HandType handType, Color color);
        void SetAimRayColor(HandType handType, Color color);
        void SetHandEnableRay(HandType handType, bool enable);
        void SetAimEnableRay(HandType handType, bool enable);
        void Shot(HandType handType, BulletData bulletData, int focusTargetId);
        void MouseAim(Vector2 mousePos);
        void IsFocusRight(bool isFocus);
        void IsFocusLeft(bool isFocus);
    }
}