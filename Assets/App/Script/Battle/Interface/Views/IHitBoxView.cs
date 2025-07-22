using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        Observable<(float damage, int enemyId)> OnHitObservable { get; }
        int Id { get; set; }
        HitBoxType HitBoxType { get; }
        void OnHit(float damage, int enemyId);
    }
}