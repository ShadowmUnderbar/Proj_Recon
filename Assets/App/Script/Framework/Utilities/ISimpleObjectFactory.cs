using UnityEngine;

namespace App.Framework.Utilities
{
    public interface ISimpleObjectFactory<TInterface>
    {
        public TInterface Instantiate(Transform parent, Vector3 position = default, Quaternion rotation = default);
    }
}