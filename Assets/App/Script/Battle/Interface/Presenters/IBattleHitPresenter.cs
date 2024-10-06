using System;
using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IBattleHitPresenter
    {
        IObservable<HitData> OnHit { get; }
    }
}