using R3;

namespace App.Battle.DataStore
{
    public interface IPlayerDataStore
    {
        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }

        bool CanLeftNormalShot { get; }
        bool CanRightNormalShot { get; }
        bool CanMergeShot { get; }
        bool CanLeftWaltzShot { get; }
        bool CanRightWaltzShot{ get; }

        //フォーカス(照準が敵に重なっている)か
        ReactiveProperty<int> IsFocusLeft { get; }
        ReactiveProperty<int> IsFocusRight { get; }

        void SetLeftNormalShotCoolDown(float time);
        void SetRightNormalShotCoolDown(float time);
        void SetMergeShotCoolDown(float time);
        void SetLeftWaltzShotCoolDown(float time);
        void SetRightWaltzShotCoolDown(float time);
    }
}