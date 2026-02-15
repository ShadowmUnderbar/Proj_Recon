using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerAimDataStore
    {
        ReactiveProperty<Pose> LeftHandPose { get; }
        ReactiveProperty<Pose> RightHandPose { get; }
        Vector3 LeftAimDirection { get; }
        Vector3 RightAimDirection { get; }

        void SetAimPosition(HandType handType, Vector3 position);
    }
}
