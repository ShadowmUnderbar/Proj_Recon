using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        public int Id { get; }
        public HitBoxType HitBoxType { get; }
    }
}