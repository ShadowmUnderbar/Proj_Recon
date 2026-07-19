using App.Battle.Data;
using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        void Init(int id, HitBoxType hitBoxType, HitDirectionType resistanceDirectionType);
        Observable<HitData> OnHitObservable { get; }
        int Id { get; }
        HitDirectionType ResistanceDirectionType { get; }
        HitBoxType HitBoxType { get; }
        /// <param name="penetrationIndex">同一弾内で何体目のヒットか（1始まり）。貫通しない攻撃は省略可</param>
        void OnHit(float damage, int attackerId, Vector3 attackCenter, out bool canPenetrable, int penetrationIndex = 1);
    }
}