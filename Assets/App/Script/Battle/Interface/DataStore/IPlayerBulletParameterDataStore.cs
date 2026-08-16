using App.Common.Data;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerBulletParameterDataStore
    {
        BulletData GetBulletData(ShotType shotType, AimFocusType focusType);

        /// <summary>
        /// パリィ弾（パリングダガー）の性能を返す。
        /// 基礎はノーマルのフォーカス射撃（即着弾）で、inheritedFormCount に応じて
        /// ノーマル→ワルツ→マージのフォーム別ダメージ強化を重ねて引き継ぐ。
        /// </summary>
        /// <param name="inheritedFormCount">引き継ぐフォーム数（1=ノーマル, 2=+ワルツ, 3=+マージ）</param>
        BulletData GetParryBulletData(int inheritedFormCount);
        void SetCoolDownTime(HandType handType, ShotType shotType, AimFocusType focusType);
        bool CanShot(HandType handType, ShotType shotType);
    }
}