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
        bool CanShotCoolDown(bool isLeft, ShotType shotType);

        float NormalFireRate { get; }
        float MergeFireRate { get; }
        float WaltzFireRate { get; }

        ReactiveProperty<int> FocusLeftTargetId { get; }
        ReactiveProperty<int> FocusRightTargetId { get; }

        void SetLeftNormalShotCoolDown(float time);
        void SetRightNormalShotCoolDown(float time);
        void SetMergeShotCoolDown(float time);
        void SetLeftWaltzShotCoolDown(float time);
        void SetRightWaltzShotCoolDown(float time);
        void SetAimPosition(bool isLeft, Vector3 position);
    }
}