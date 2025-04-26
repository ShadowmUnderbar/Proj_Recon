using App.Common.Interface.Views;
using UnityEngine;

namespace App.Common.Views
{
    [RequireComponent(typeof(LineRenderer))]
    public class ForwardRayView : MonoBehaviour, IForwardRayView
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LayerMask _layerMask;

        private Material _material;

        private float _maxRayRange = 50f;

        private void Awake()
        {
            _material = _lineRenderer.material;
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

            var ray = new Ray(transform.position, transform.forward);

            _lineRenderer.SetPosition(0, transform.position);

            _lineRenderer.SetPosition(1, transform.position + transform.forward * _maxRayRange);
        }

        public void SetRayColor(Color color)
        {
            if (_material == null)
            {
                return;
            }

            _material.color = color;
        }
        
        public void SetEnable(bool enable)
        {
            _lineRenderer.enabled = enable;
        }
    }
}