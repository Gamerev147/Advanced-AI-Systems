using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        Vector3 dir = (transform.position - _cam.transform.position);
        transform.DOLookAt(transform.position + dir, 0.1f, AxisConstraint.Y);
    }
}
