using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        public uint Id { get; set; }
        public HitBoxType HitBoxType { get; }
    }
}