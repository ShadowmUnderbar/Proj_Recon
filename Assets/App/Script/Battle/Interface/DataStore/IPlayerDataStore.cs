using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerDataStore
    {
        bool IsFocusInput { get; set; }
        UnlockCoreSkillType UnlockCoreSkillType { get; }
        ReactiveProperty<Vector3> Position { get; }
        ReactiveProperty<Quaternion> Rotate { get; }
        Pose Pose { get; }
        Transform PlayerTransform { get; }

        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }

        ReactiveProperty<ShotType> ShotType { get; }
        ReactiveProperty<AimFocusType> LeftFocusType { get; }
        ReactiveProperty<AimFocusType> RightFocusType { get; }

        ReactiveProperty<int> FocusLeftTargetId { get; }
        ReactiveProperty<int> FocusRightTargetId { get; }

        ReactiveProperty<Pose> LeftHandPose { get; }
        ReactiveProperty<Pose> RightHandPose { get; }
        Vector3 LeftAimDirection { get; }
        Vector3 RightAimDirection { get; }
        
        ReactiveProperty<bool> IsLeftFocusInput { get; }
        ReactiveProperty<bool> IsRightFocusInput { get; }

        float MoveSpeed { get; }

        void SetAimPosition(HandType handType, Vector3 position);
        void Move(Vector2 moveV2, float speed);
        void SetUnlockCoreSkillType(UnlockCoreSkillType unlockCoreSkillType);
    }
}