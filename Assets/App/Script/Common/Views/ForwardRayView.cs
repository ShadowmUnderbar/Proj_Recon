using App.Common.Data;
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

            var start = transform.position;
            var end = start + transform.forward * GameParamData.RayMaxDistance;

            // カーブ有効時は途中に点を足す。2点のままだと両端しか沈まず、間が浮く
            CurvedWorldLine.SetLine(_lineRenderer, start, end);
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