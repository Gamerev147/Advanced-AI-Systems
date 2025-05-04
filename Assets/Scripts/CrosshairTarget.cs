using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrosshairTarget : MonoBehaviour
{
    private Camera _mainCam;

    private Ray _ray;
    private RaycastHit _hit;

    private void Start()
    {
        _mainCam = Camera.main;
    }

    private void Update()
    {
        _ray.origin = _mainCam.transform.position;
        _ray.direction = _mainCam.transform.forward;
        Physics.Raycast(_ray, out _hit);

        transform.position = _hit.point;
    }
}
