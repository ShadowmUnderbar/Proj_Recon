using System.Collections.Generic;
using App.Battle.Data;
using App.Battle.Interface;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class HitBoxStoreView : MonoBehaviour, IHitBoxStoreView
    {
        private readonly Dictionary<int, IHitBoxView> _hitBoxViews = new();

        private readonly Subject<HitData> _onHitObservable = new();
        public Observable<HitData> OnHitObservable => _onHitObservable;

        private readonly CompositeDisposable _disposables = new();

        public void AddHitBoxView(IHitBoxView hitBoxView)
        {
            _hitBoxViews.Add(hitBoxView.Id, hitBoxView);

            hitBoxView.OnHitObservable
                .Subscribe(OnHit)
                .AddTo(_disposables);
        }

        private void OnHit(HitData hitData)
        {
            Debug.Log("HitBoxStoreView.OnHit: " + hitData);
            _onHitObservable.OnNext(hitData);
        }

        public void RemoveHitBoxView(int id)
        {
            _hitBoxViews.Remove(id);
        }

        public void OnDestroy()
        {
            _disposables.Dispose();
            _hitBoxViews.Clear();
            _onHitObservable.Dispose();
        }
    }
}