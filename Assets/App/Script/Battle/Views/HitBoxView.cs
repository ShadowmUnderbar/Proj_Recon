using App.Battle.Data;
using App.Battle.Interface;
using App.Framework.Utilities.Extensions;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class HitBoxView : MonoBehaviour, IHitBoxView
    {
        private readonly Subject<HitData> _onHitObservable = new();
        public Observable<HitData> OnHitObservable => _onHitObservable;

        [SerializeField] private HitBoxType hitBoxType;

        public HitBoxType HitBoxType => hitBoxType;

        public int Id { get; set; }

        public void OnHit(float damage, int attackerId, Vector3 attackCenter)
        {
            var normalizedHitDirection = (transform.position - attackCenter).normalized.ToTopdown();

            var hitData = new HitData(
                attackerId,
                damage,
                normalizedHitDirection
            );
            _onHitObservable.OnNext(hitData);
        }
    }
}