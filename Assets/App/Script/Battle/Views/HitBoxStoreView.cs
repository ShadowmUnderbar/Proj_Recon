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

        public IEnumerator<IHitBoxView> HitBoxViews => _hitBoxViews.Values.GetEnumerator();

        // 敵ごとの購読。敵が消えたときに一緒に解除しないと、1ランで湧いた数だけ溜まり続ける。
        // 1体に複数のヒットボックスがあっても同じ敵IDでまとめて持つ
        private readonly Dictionary<int, CompositeDisposable> _subscriptions = new();

        public void AddHitBoxView(IHitBoxView hitBoxView)
        {
            // 一覧は敵IDにつき1つ（最初のヒットボックス）だけ持つ。2つ目以降で例外にしない
            _hitBoxViews.TryAdd(hitBoxView.Id, hitBoxView);

            if (!_subscriptions.TryGetValue(hitBoxView.Id, out var subscriptions))
            {
                subscriptions = new CompositeDisposable();
                _subscriptions.Add(hitBoxView.Id, subscriptions);
            }

            hitBoxView.OnHitObservable
                .Subscribe(OnHit)
                .AddTo(subscriptions);
        }

        private void OnHit(HitData hitData)
        {
            _onHitObservable.OnNext(hitData);
        }

        public void RemoveHitBoxView(int id)
        {
            _hitBoxViews.Remove(id);

            if (_subscriptions.Remove(id, out var subscriptions))
            {
                subscriptions.Dispose();
            }
        }

        public void OnDestroy()
        {
            foreach (var subscriptions in _subscriptions.Values)
            {
                subscriptions.Dispose();
            }

            _subscriptions.Clear();
            _hitBoxViews.Clear();
            _onHitObservable.Dispose();
        }
    }
}