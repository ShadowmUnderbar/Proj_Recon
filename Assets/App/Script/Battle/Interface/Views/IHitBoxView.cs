using App.Battle.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        Observable<HitData> OnHitObservable { get; }
        int Id { get; set; }
        HitBoxType HitBoxType { get; }
        void OnHit(float damage, int attackerId, Vector3 attackCenter);
    }
}