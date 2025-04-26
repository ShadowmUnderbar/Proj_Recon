using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBattlePlayerView
    {
        Observable<HitData> OnHit { get; }

        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }
        Observable<Vector3> OnRightAimPosition { get; }
        Observable<Vector3> OnLeftAimPosition { get; }
        ReactiveProperty<Vector3> OnUpdatePosition { get; }
        ReactiveProperty<Pose> LeftHandPose { get; }
        ReactiveProperty<Pose> RightHandPose { get; }

        void Move(Vector2 inputV2, float speed);
        void Aim();
        void SetRayColor(HandType handType, Color color);
        void MouseAim(Vector2 mousePos);
        void Shot(HandType handType, ShotType shotType, AimFocusType focusType, int focusTargetId);
    }
}