using App.Common.Data;
using R3;

namespace App.Battle.DataStore
{
    public interface IPlayerDataStore
    {
        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }

        bool CanShotCoolDown(bool isLeft, ShotType shotType);

        float NormalFireRate { get; }
        float MergeFireRate { get; }
        float WaltzFireRate { get; }

        //フォーカス(照準が敵に重なっている)か
        ReactiveProperty<int> FocusLeftTargetId { get; }
        ReactiveProperty<int> FocusRightTargetId { get; }

        void SetLeftNormalShotCoolDown(float time);
        void SetRightNormalShotCoolDown(float time);
        void SetMergeShotCoolDown(float time);
        void SetLeftWaltzShotCoolDown(float time);
        void SetRightWaltzShotCoolDown(float time);

        ShotType GetShotType(bool isLeft);
    }
}