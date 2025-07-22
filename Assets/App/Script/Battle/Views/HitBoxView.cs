using App.Battle.Data;
using App.Battle.Interface;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class HitBoxView : MonoBehaviour, IHitBoxView
    {
        private readonly Subject<(float damage, int enemyId)> _onHitObservable = new();
        public Observable<(float damage, int enemyId)> OnHitObservable => _onHitObservable;

        [SerializeField] private HitBoxType hitBoxType;

        public HitBoxType HitBoxType => hitBoxType;

        public int Id { get; set; }

        public void OnHit(float damage, int enemyId)
        {
            _onHitObservable.OnNext((damage, enemyId));
        }
    }
}