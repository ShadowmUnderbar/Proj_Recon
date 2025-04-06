using App.Battle.Data;
using App.Battle.Interface;
using App.Common.Data;
using App.Common.Views;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerTopDownAimView : MonoBehaviour, IPlayerTopDownAimView
    {
        private PlatformHandRotation _platformHandRotation;
        private const float Radius = 0.2f;
        private const float Distance = 50f;
        private RaycastHit[] raycastHits = new RaycastHit[5];
        private readonly int TargetLayers = LayerConstants.Default | LayerConstants.Hitbox;

        public Observable<int> OnFocus => _onFocus;
        private Subject<int> _onFocus = new();

        private void Start()
        {
            _platformHandRotation = transform.parent.GetComponent<PlatformHandRotation>();
        }

        public Vector3 GetAimPosition()
        {
            if (_platformHandRotation == null)
            {
                return transform.forward * Distance;
            }

            var count = Physics.SphereCastNonAlloc(transform.position, Radius,
                _platformHandRotation.Rotation * Vector3.forward,
                raycastHits, Distance,
                TargetLayers);

            if (count <= 0)
            {
                _onFocus.OnNext(-1);
                return transform.forward * Distance;
            }

            for (var i = 0; i < count; i++)
            {
                var hit = raycastHits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                var view = hit.collider.GetComponent<IHitBoxView>();

                if (view == null || view.HitBoxType == HitBoxType.Player)
                {
                    continue;
                }

                if (view.HitBoxType == HitBoxType.Enemy)
                {
                    _onFocus.OnNext((int)view.Id);
                    return hit.collider.transform.position;
                }

                _onFocus.OnNext(-1);
                return hit.point;
            }

            _onFocus.OnNext(-1);
            return raycastHits[0].point;
        }
    }
}