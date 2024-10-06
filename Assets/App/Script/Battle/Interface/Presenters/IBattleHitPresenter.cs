using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IBattleHitPresenter
    {
        Observable<HitData> OnHit { get; }
    }
}