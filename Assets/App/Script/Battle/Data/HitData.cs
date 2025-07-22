using UnityEngine;

namespace App.Battle.Data
{
    public class HitData
    {
        public HitData(int damagedId, float damage, Vector2 normalizedHitDirection)
        {
            DamagedId = damagedId;
            Damage = damage;
            NormalizedHitDirection = normalizedHitDirection;
        }

        public int DamagedId { get; set; }
        public float Damage { get; set; }
        public Vector2 NormalizedHitDirection { get; set; }
    }
}