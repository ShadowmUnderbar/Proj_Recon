using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IBulletView
    {
        Observable<HitData> OnHit { get; }
        void Spawn(Pose pose, BulletData bulletData);
    }
}