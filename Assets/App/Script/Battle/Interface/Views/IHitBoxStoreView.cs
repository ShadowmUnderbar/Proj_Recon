using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IHitBoxStoreView
    {
        Observable<HitData> OnHitObservable { get; }
        void AddHitBoxView(IHitBoxView hitBoxView);
        void RemoveHitBoxView(int id);
    }
}