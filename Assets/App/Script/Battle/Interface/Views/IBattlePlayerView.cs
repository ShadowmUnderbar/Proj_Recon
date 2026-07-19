using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBattlePlayerView
    {
        Transform PlayerTransform { get; }
        Observable<HitData> OnHit { get; }

        /// <summary>被弾したダメージ量を流す（被弾受けコンポーネント由来）</summary>
        Observable<float> OnDamaged { get; }

        Observable<int> OnFocusLeft { get; }
        Observable<int> OnFocusRight { get; }
        Observable<Vector3> OnRightAimPosition { get; }
        Observable<Vector3> OnLeftAimPosition { get; }
        ReactiveProperty<Vector3> OnUpdatePosition { get; }
        ReactiveProperty<Pose> LeftHandPose { get; }
        ReactiveProperty<Pose> RightHandPose { get; }

        void IsFocusLeft(bool isFocus);
        void IsFocusRight(bool isFocus);

        void Move(Vector2 inputV2);
        void SetMoveAnimation(Vector2 dir);
        void SetModelFacing(Vector3 dir);
        void SetAimTargets(Vector3 leftTarget, Vector3 rightTarget);
        void Aim();
        void MouseAim(Vector2 mousePos);
        void SetAimRayColor(HandType handType, Color color);
        void SetAimEnableRay(HandType handType, bool enable);
        void SetHandRayColor(HandType handType, Color color);
        void SetHandEnableRay(HandType handType, bool enable);
        void Shot(HandType handType, BulletData bulletData, int focusTargetId);
        void Blitz(Vector3 startPos, Transform playerPos);
    }
}