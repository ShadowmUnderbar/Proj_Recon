using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace App.Framework.Utilities
{
    public sealed class SimpleObjectFactory<TInterface, TView> : ISimpleObjectFactory<TInterface> where TView : MonoBehaviour, TInterface
    {
        private readonly TView _prefab;
        private readonly IObjectResolver _resolver;

        [Inject]
        public SimpleObjectFactory(IObjectResolver resolver, TView prefab)
        {
            _resolver = resolver;
            _prefab = prefab;
        }

        public TInterface Instantiate(Transform parent, Vector3 position = default, Quaternion rotation = default)
        {
            var go = _resolver.Instantiate(_prefab.gameObject, parent);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            var t = go.GetComponent<TInterface>();
            return t;
        }
    }
}