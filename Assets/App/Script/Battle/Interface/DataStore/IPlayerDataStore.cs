using App.Common.Data;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerDataStore
    {
        ReactiveProperty<Vector3> Position { get; }
        ReactiveProperty<Quaternion> Rotate { get; }
        Pose Pose { get; }

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

        float MoveSpeed { get; }

        void SetAimPosition(HandType handType, Vector3 position);
        void Move(Vector2 moveV2, float speed);
    }
}