using App.Battle.Interface.Views;
using UniRx;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerTopDownAimView : MonoBehaviour, IPlayerTopDownAimView
    {
        private const float Radius = 0.5f;
        private const float Distance = 50f;
        private RaycastHit[] raycastHits = new RaycastHit[5];
        private readonly int TargetLayers = LayerConstants.Default | LayerConstants.Hitbox;

        public ReactiveProperty<bool> IsFocus = new ();


        public Vector3 GetAimPosition()
        {
            var count = Physics.SphereCastNonAlloc(transform.position, Radius, transform.forward, raycastHits, Distance, TargetLayers);

            if (count <= 0)
            {
                IsFocus.Value = false;
                return transform.forward * Distance;
            }

            foreach (var hit in raycastHits)
            {
                var view = hit.collider.GetComponent<IHitBoxView>();

                if (view == null || view.HitBoxType == HitBoxType.Player)
                {
                    continue;
                }


                if (view.HitBoxType == HitBoxType.Enemy)
                {
                    IsFocus.Value = true;
                    return hit.collider.transform.position;
                }

                IsFocus.Value = false;
                return hit.point;
            }

            return transform.forward * Distance;
        }
    }
}