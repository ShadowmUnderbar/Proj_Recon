using App.Common.Data;
using R3;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerBulletParameterDataStore
    {
        /// <summary>
        /// プレイヤーが撃った（クールダウンを開始した）ときに、撃った手とフォームが流れる。
        /// 射撃をきっかけにする効果（サモンタレット）が購読する
        /// </summary>
        Observable<(HandType handType, ShotType shotType)> OnShot { get; }

        BulletData GetBulletData(ShotType shotType, AimFocusType focusType);
        void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType);
        bool CanShot(HandType handType, ShotType shotType);
    }
}
