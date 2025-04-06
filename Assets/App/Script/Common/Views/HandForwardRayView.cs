using App.Common.Interface;
using UnityEngine;

namespace App.Common.Views
{
    [RequireComponent(typeof(LineRenderer))]
    public class HandForwardRayView : MonoBehaviour, IForwardRayView
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LayerMask _layerMask;
        private PlatformHandRotation _platformHandRotation;

        private float _maxRayRange = 50f;

        private void Start()
        {
            _platformHandRotation = GetComponent<PlatformHandRotation>();
        }

        public void Close()
        {
            _lineRenderer.enabled = false;
        }

        public void View()
        {
            _lineRenderer.enabled = true;
        }

        private void Update()
        {
            if (!_lineRenderer.enabled)
            {
                return;
            }

            if (!_platformHandRotation)
            {
                return;
            }

            var direction = _platformHandRotation.Rotation * Vector3.forward;
            var ray = new Ray(transform.position, direction);

            _lineRenderer.SetPosition(0, transform.position);

            _lineRenderer.SetPosition(1, transform.position + direction * _maxRayRange);
        }
    }
}