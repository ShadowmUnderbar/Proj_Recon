using App.Battle.Interface.Views;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerTopDownAimView : MonoBehaviour, IPlayerTopDownAimView
    {
        [SerializeField]
        private LayerMask _layerMask;

        private const float RayRadius = 0.5f;


        public Vector3 GetAimPosition()
        {
            if (!Physics.SphereCast(transform.position, RayRadius, transform.forward, out var hit))
            {
                return transform.forward * 50f;
            }

            return hit.point;
        }

        public bool IsFocus()
        {
            return false;
        }
    }
}