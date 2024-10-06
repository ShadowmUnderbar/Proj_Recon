using System;
using App.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBulletView
    {
        IObservable<HitData> OnHit { get; }
        void Spawn(Pose pose, BulletData bulletData);
    }
}