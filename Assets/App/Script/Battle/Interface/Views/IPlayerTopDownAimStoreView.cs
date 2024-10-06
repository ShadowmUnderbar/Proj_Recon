using System;
using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimStoreView
    {
        Observable<HitData> OnHit { get; }

        Observable<int> OnFocus { get; }
        Observable<int> OnUnFocus { get; }

        void Aim();
        void Shot(bool isLeft);
    }
}