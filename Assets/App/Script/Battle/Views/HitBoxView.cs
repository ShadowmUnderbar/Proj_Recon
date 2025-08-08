using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using App.Framework.Utilities.Extensions;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class HitBoxView : MonoBehaviour, IHitBoxView
    {
        private readonly Subject<HitData> _onHitObservable = new();

        public void Init(int id, HitBoxType hitBoxType, HitDirectionType resistanceDirectionType)
        {
            Id = id;
            HitBoxType = hitBoxType;
            ResistanceDirectionType = resistanceDirectionType;
        }

        public Observable<HitData> OnHitObservable => _onHitObservable;

        public int Id { get; private set; }
        public HitDirectionType ResistanceDirectionType { get; private set; }
        public HitBoxType HitBoxType { get; private set; }

        public void OnHit(float damage, int attackerId, Vector3 attackCenter, out bool canPenetrable)
        {
            var directionType = RelativeYawExtension.GetActorRelative(transform.ToPose(), attackCenter);

            var hitData = new HitData(
                Id,
                damage,
                directionType
            );
            _onHitObservable.OnNext(hitData);

            canPenetrable = directionType != ResistanceDirectionType;
        }
    }
}