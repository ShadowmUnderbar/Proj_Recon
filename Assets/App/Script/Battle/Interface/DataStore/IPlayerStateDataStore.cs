using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerStateDataStore
    {
        ReactiveProperty<Vector3> Position { get; }
        ReactiveProperty<Quaternion> Rotate { get; }
        Pose Pose { get; }
        Transform PlayerTransform { get; set; }

        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }

        float MoveSpeed { get; }
        UnlockCoreSkillType UnlockCoreSkillType { get; }

        void Move(Vector2 moveV2, float speed);
        void SetUnlockCoreSkillType(UnlockCoreSkillType unlockCoreSkillType);
    }
}