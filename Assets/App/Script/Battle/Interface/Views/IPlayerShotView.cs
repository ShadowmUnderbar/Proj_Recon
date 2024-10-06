using System;
using App.Battle.Data;

namespace App.Battle.Interface
{
    public interface IPlayerShotView
    {
        IObservable<HitData> OnHit { get; }

        void SpawnBullet();
    }
}