
using App.Common.Data;
using UnityEngine;

namespace App.Battle.Data
{
    public class HitData
    {
        public HitData(int damagedId, float damage, HitDirectionType hitDirectionType,
            Vector3 hitDirection = default)
        {
            DamagedId = damagedId;
            Damage = damage;
            HitDirectionType = hitDirectionType;
            HitDirection = hitDirection;
        }

        public int DamagedId { get; set; }
        public float Damage { get; set; }
        public HitDirectionType HitDirectionType { get; set; }

        /// <summary>
        /// 被弾の水平方向（弾→敵）。撃たれた方向と逆＝傾けたい向きを表す。
        /// 演出用のため、ダメージ計算では参照しない。
        /// </summary>
        public Vector3 HitDirection { get; set; }
    }
}