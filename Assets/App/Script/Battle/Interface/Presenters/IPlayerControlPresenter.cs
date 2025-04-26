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

        void Move(Vector2 moveV2, float speed);
        void Aim();
        void SetRayColor(HandType handType, Color color);
        void Shot(HandType handType, ShotType shotType, AimFocusType focusType, int focusTargetId);
        void MouseAim(Vector2 mousePos);
    }
}