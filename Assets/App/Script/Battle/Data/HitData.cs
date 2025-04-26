using UnityEngine;

namespace App.Battle.Data
{
    public class HitData
    {
        public int DamagedId { get; set; }
        public float Damage { get; set; }
        public Vector2 NormalizedHitDirection { get; set; }
    }
}