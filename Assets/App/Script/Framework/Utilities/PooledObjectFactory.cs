using UnityEngine;
using VContainer;

namespace App.Framework.Utilities
{
    /// <summary>
    /// <see cref="SimpleObjectFactory{TInterface,TView}"/> と同じ窓口で、生成の代わりにプールから貸し出す。
    /// 返却は View 自身が <see cref="IViewReturner.Return"/> で行う（Destroy しない）。
    /// 非表示の置き場として自分用の GameObject を1つ作る（シーンと一緒に消える）
    /// </summary>
    public sealed class PooledObjectFactory<TInterface, TView> : ISimpleObjectFactory<TInterface>
        where TView : MonoBehaviour, TInterface, IPoolableView
    {
        private readonly ViewPool<TView> _pool;

        [Inject]
        public PooledObjectFactory(IObjectResolver resolver, TView prefab, int initialCount)
        {
            var root = new GameObject($"Pool_{prefab.name}");
            _pool = new ViewPool<TView>(resolver, prefab, root.transform, initialCount);
        }

        public TInterface Instantiate(Transform parent, Vector3 position = default, Quaternion rotation = default)
        {
            var view = _pool.Rent();
            if (parent != null)
            {
                view.transform.SetParent(parent, false);
            }

            view.transform.localPosition = position;
            view.transform.localRotation = rotation;
            return view;
        }
    }
}
