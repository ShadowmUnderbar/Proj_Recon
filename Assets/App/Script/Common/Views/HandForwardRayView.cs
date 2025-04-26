using UnityEngine;

namespace App.Common.Views
{
    [RequireComponent(typeof(LineRenderer))]
    public class HandForwardRayView : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LayerMask _layerMask;
        private PlatformHandRotation _platformHandRotation;
        private readonly string _emissiveColor = "_EmissionColor";
        private readonly float _maxRayRange = 50f;

        private Material _material;

        private void Start()
        {
            _platformHandRotation = GetComponent<PlatformHandRotation>();
            _material = _lineRenderer.material;
        }

        public void SetEnable(bool enable)
        {
            _lineRenderer.enabled = enable;
        }

        public void SetRayColor(Color color)
        {
            if (_material == null)
            {
                return;
            }

            color *= 1.5f;
            color.a = 0.75f;
            _material.color = color;
            _material.SetColor(_emissiveColor, color);
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

            _lineRenderer.SetPosition(0, transform.position);

            _lineRenderer.SetPosition(1, transform.position + direction * _maxRayRange);
        }
    }
}