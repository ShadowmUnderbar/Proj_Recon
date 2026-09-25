using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        void Init(int id, HitBoxType hitBoxType, HitDirectionType resistanceDirectionType);
        Observable<HitData> OnHitObservable { get; }
        int Id { get; }
        HitDirectionType ResistanceDirectionType { get; }
        HitBoxType HitBoxType { get; }
        /// <param name="penetrationIndex">同一弾内で何体目のヒットか（1始まり）。貫通しない攻撃は省略可</param>
        /// <param name="shotType">命中を発生させたプレイヤーの射撃フォーム。射撃以外の攻撃は省略可（null）</param>
        /// <param name="focusType">命中を発生させた弾のエイム状態。射撃以外の攻撃は省略可（NotFocus）</param>
        /// <param name="isFocusTarget">命中先がその弾のフォーカス対象そのものか</param>
        /// <param name="isProjectile">弾の直撃による命中か（近接攻撃・爆風は false）。跳ね返し対象の判定に使う</param>
        /// <param name="projectileId">
        /// 命中を発生させた弾の一意なId（弾以外は0）。
        /// 同一の弾を重複してカウントしないための識別に使う（回避時跳ね返し攻撃の接触弾カウント）
        /// </param>
        void OnHit(float damage, int attackerId, Vector3 attackCenter, out bool canPenetrable, int penetrationIndex = 1,
            ShotType? shotType = null, AimFocusType focusType = AimFocusType.NotFocus, bool isFocusTarget = false,
            bool isProjectile = false, int projectileId = 0);
    }
}