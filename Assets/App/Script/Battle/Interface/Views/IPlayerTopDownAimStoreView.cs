using System;
using App.Script.Battle.Data;

namespace App.Battle.Interface.Views
{
    public interface IPlayerTopDownAimStoreView
    {
        IObservable<HitData> OnHit { get; }
        void Aim();
        void Shot(bool isLeft);
        bool SetFocus(bool isLeft);
    }
}