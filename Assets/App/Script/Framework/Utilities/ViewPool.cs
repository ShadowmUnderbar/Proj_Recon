using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Framework.Utilities
{
    /// <summary>
    /// プレハブ1種類ぶんの View のプール。
    /// 生成は DI コンテナ経由（[Inject] メソッドが効く）で、足りなければ増やし、返された View は非表示で親の下に置く。
    /// 貸出中の一覧を持つので、「場にある全部」に対する操作（停止・一括消去）はタグ検索をせずに済む
    /// </summary>
    public sealed class ViewPool<TView> : IViewReturner where TView : MonoBehaviour, IPoolableView
    {
        private readonly IObjectResolver _resolver;
        private readonly TView _prefab;
        private readonly Transform _inactiveParent;

        private readonly Stack<TView> _inactive = new();
        private readonly List<TView> _actives = new();

        // 返却中の一括処理で列挙を壊さないための作業用
        private readonly List<TView> _returnBuffer = new();

        /// <summary>貸出中の View。列挙中に返却されると壊れるので、まとめて返すときは <see cref="ReturnAll"/> を使う</summary>
        public IReadOnlyList<TView> Actives => _actives;

        /// <param name="initialCount">最初に作っておく数。ウェーブ中の生成を減らすために使う</param>
        public ViewPool(IObjectResolver resolver, TView prefab, Transform inactiveParent, int initialCount)
        {
            _resolver = resolver;
            _prefab = prefab;
            _inactiveParent = inactiveParent;

            for (var i = 0; i < initialCount; i++)
            {
                _inactive.Push(Create());
            }
        }

        public TView Rent()
        {
            var view = _inactive.Count > 0 ? _inactive.Pop() : Create();

            // 親から外してから表示する（親の非表示に引きずられないように）
            view.transform.SetParent(null, false);
            view.gameObject.SetActive(true);
            _actives.Add(view);
            view.OnRent(this);
            return view;
        }

        public void Return(IPoolableView poolable)
        {
            if (poolable is not TView view)
            {
                return;
            }

            // 二重返却（寿命切れと着弾が同じフレームに重なる等）は無視する
            if (!_actives.Remove(view))
            {
                return;
            }

            view.OnReturn();
            view.gameObject.SetActive(false);
            view.transform.SetParent(_inactiveParent, false);
            _inactive.Push(view);
        }

        public void ReturnAll()
        {
            _returnBuffer.Clear();
            _returnBuffer.AddRange(_actives);
            foreach (var view in _returnBuffer)
            {
                Return(view);
            }

            _returnBuffer.Clear();
        }

        private TView Create()
        {
            var go = _resolver.Instantiate(_prefab.gameObject, _inactiveParent);
            go.SetActive(false);
            return go.GetComponent<TView>();
        }
    }
}
