
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    public class HitData
    {
        public HitData(int damagedId, float damage, HitDirectionType hitDirectionType,
            Vector3 hitDirection = default, int penetrationIndex = 1, ShotType? shotType = null,
            AimFocusType focusType = AimFocusType.NotFocus, bool isFocusTarget = false)
        {
            DamagedId = damagedId;
            Damage = damage;
            HitDirectionType = hitDirectionType;
            HitDirection = hitDirection;
            PenetrationIndex = penetrationIndex;
            ShotType = shotType;
            FocusType = focusType;
            IsFocusTarget = isFocusTarget;
        }

        public int DamagedId { get; set; }
        public float Damage { get; set; }
        public HitDirectionType HitDirectionType { get; set; }

        /// <summary>
        /// 被弾の水平方向（弾→敵）。撃たれた方向と逆＝傾けたい向きを表す。
        /// 演出用のため、ダメージ計算では参照しない。
        /// </summary>
        public Vector3 HitDirection { get; set; }

        /// <summary>
        /// この命中が同一弾内で何体目のヒットか（1始まり）。
        /// 貫通しない攻撃は常に1。PenetrationCount条件バフのダメージ倍率計算に使う。
        /// </summary>
        public int PenetrationIndex { get; set; }

        /// <summary>
        /// この命中を発生させたプレイヤーの射撃フォーム。
        /// 射撃以外（回避の突進ダメージ・敵の攻撃）は null。
        /// フォームを参照するバフ・アップグレード（ドーパミン／雪崩）の判定に使う。
        /// </summary>
        public ShotType? ShotType { get; set; }

        /// <summary>
        /// この命中を発生させた弾のエイム状態（フォーカス射撃か否か）。
        /// 射撃以外（回避の突進ダメージ・敵の攻撃）は NotFocus。
        /// </summary>
        public AimFocusType FocusType { get; set; }

        /// <summary>
        /// 命中先がこの弾のフォーカス対象そのものか。
        /// フォーカス弾が対象以外へ当たったケースを区別する（キリングコールの条件判定に使う）。
        /// </summary>
        public bool IsFocusTarget { get; set; }
    }
}