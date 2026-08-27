using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerBulletParameterDataStore
    {
        BulletData GetBulletData(ShotType shotType, AimFocusType focusType);
        void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType);
        bool CanShot(HandType handType, ShotType shotType);
    }
}