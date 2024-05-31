using System;
using App.Script.Battle.Data;

namespace App.Battle.Interface.Views
{
    public interface IPlayerShotView
    {
        IObservable<HitData> OnHit { get; }

        void SpawnBullet();
    }
}