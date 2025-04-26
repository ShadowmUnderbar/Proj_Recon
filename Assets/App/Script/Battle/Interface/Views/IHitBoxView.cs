using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        public int Id { get; set; }
        public HitBoxType HitBoxType { get; }
    }
}