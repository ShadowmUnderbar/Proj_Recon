using UnityEngine;

namespace App.Script.Battle.Data
{
    public class HitData
    {
        public uint DamagedId { get; set; }
        public float Damage { get; set; }
        public Vector2 NormalizedHitDirection { get; set; }
    }
}