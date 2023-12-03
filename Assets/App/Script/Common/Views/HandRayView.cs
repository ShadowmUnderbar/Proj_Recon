using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HandRayView : MonoBehaviour , IHandRayView
{
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private LayerMask _layerMask;

    private float _maxRayRange = 50f;

    private void Awake()
    {
        _lineRenderer.SetPosition(0, transform.position);
    }

    public void Close()
    {
        _lineRenderer.enabled = false;
    }

    public void View()
    {
        _lineRenderer.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_lineRenderer.enabled)
        {
            return;
        }

        var ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out var hit, _maxRayRange,_layerMask))
        {
            _lineRenderer.SetPosition(1, hit.point);
        }

        _lineRenderer.SetPosition(1, transform.forward * _maxRayRange);
    }
}
