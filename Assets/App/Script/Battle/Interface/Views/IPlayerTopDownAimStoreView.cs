using System;
using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimStoreView
    {
        IObservable<HitData> OnHit { get; }
        void Aim();
        void Shot(bool isLeft);
        bool SetFocus(bool isLeft);
    }
}