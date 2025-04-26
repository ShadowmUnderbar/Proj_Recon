using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerDataStore
    {
        ReactiveProperty<Vector3> Position { get; }
        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }

        ReactiveProperty<ShotType> ShotType { get; }
        ReactiveProperty<AimFocusType> LeftFocusType { get; }
        ReactiveProperty<AimFocusType> RightFocusType { get; }

        ReactiveProperty<int> FocusLeftTargetId { get; }
        ReactiveProperty<int> FocusRightTargetId { get; }

        bool CanLeftShot { get; }
        bool CanRightShot { get; }

        void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType);

        void SetAimPosition(HandType handType, Vector3 position);
    }
}