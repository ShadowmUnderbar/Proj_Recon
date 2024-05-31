using System;
using App.Script.Battle.Data;

namespace App.Battle.Interface.Presenters
{
    public interface IBattleHitPresenter
    {
        IObservable<HitData> OnHit { get; }
    }
}