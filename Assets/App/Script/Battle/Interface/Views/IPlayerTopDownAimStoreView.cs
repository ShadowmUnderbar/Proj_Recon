using System;
using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimStoreView
    {
        Observable<HitData> OnHit { get; }
        void Aim();
        void Shot(bool isLeft);
        void SetFocus(bool isLeft);
        void SetUnFocus(bool isLeft);
    }
}