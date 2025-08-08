
using App.Common.Data;

namespace App.Battle.Data
{
    public class HitData
    {
        public HitData(int damagedId, float damage, HitDirectionType hitDirectionType)
        {
            DamagedId = damagedId;
            Damage = damage;
            HitDirectionType = hitDirectionType;
        }

        public int DamagedId { get; set; }
        public float Damage { get; set; }
        public HitDirectionType HitDirectionType { get; set; }
    }
}