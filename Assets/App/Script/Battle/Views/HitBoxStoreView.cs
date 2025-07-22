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
            if (hitBoxView == null)
            {
                return;
            }

            if (!_hitBoxViews.ContainsKey(hitBoxView.Id))
            {
                return;
            }

            _hitBoxViews.Add(hitBoxView.Id, hitBoxView);

            hitBoxView.OnHitObservable
                .Subscribe(x => _onHitObservable.OnNext(x))
                .AddTo(_disposables);
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