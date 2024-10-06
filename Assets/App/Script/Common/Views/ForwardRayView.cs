using App.Common.Interface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ForwardRayView : MonoBehaviour, IForwardRayView
{
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private LayerMask _layerMask;

    private float _maxRayRange = 50f;


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

        _lineRenderer.SetPosition(0, transform.position);


        _lineRenderer.SetPosition(1, transform.position + transform.forward * _maxRayRange);
    }
}