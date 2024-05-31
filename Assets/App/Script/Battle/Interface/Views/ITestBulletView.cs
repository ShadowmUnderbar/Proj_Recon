using System;
using App.Script.Battle.Data;
using UnityEngine;

namespace App.Battle.Interface.Views
{
    public interface IBulletView
    {
        IObservable<HitData> OnHit { get; }
        void Spawn(Pose pose, BulletData bulletData);
    }
}