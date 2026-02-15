using System;
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
        private readonly RaycastHit[] _raycastHits = new RaycastHit[5];

        public Observable<int> OnFocus => _onFocus;
        private readonly Subject<int> _onFocus = new();

        public bool IsFocus { get; set; }
        public Transform Transform => transform;

        private void Start()
        {
            _platformHandRotation = transform.parent.GetComponent<PlatformHandRotation>();
        }

        private Vector3 DefaultAimPosition => transform.forward * GameParamData.RayMaxDistance;

        public Vector3 GetAimPosition()
        {
            if (_platformHandRotation == null)
            {
                _onFocus.OnNext(-1);
                return DefaultAimPosition;
            }

            if (!Physics.Raycast(transform.position,
                    _platformHandRotation.Rotation * Vector3.forward,
                    out var hitGround,
                    GameParamData.RayMaxDistance, LayerConstants.Default))
            {
                _onFocus.OnNext(-1);
                return DefaultAimPosition;
            }

            if (!IsFocus)
            {
                _onFocus.OnNext(-1);
                return hitGround.point;
            }

            var count = Physics.SphereCastNonAlloc(transform.position, Radius,
                _platformHandRotation.Rotation * Vector3.forward,
                _raycastHits, GameParamData.RayMaxDistance,
                LayerConstants.Hitbox);

            if (count <= 0)
            {
                _onFocus.OnNext(-1);
                var pos = hitGround.point;
                pos.y = 0;
                return pos;
            }

            for (var i = 0; i < count; i++)
            {
                var hit = _raycastHits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                var view = hit.collider.GetComponent<IHitBoxView>();

                if (view == null || view.HitBoxType == HitBoxType.Player)
                {
                    continue;
                }

                if (view.HitBoxType != HitBoxType.Enemy)
                {
                    continue;
                }

                _onFocus.OnNext(view.Id);
                return hit.collider.transform.position;
            }

            _onFocus.OnNext(-1);
            return hitGround.point;
        }

        private void OnDestroy()
        {
            _onFocus.Dispose();
        }
    }
}