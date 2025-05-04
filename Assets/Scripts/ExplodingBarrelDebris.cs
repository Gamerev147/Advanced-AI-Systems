using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBarrelDebris : MonoBehaviour
{
    private Rigidbody[] rbs;

    private void Awake()
    {
        rbs = GetComponentsInChildren<Rigidbody>();
    }

    private void Start()
    {
        foreach (var rb in rbs)
        {
            rb.AddExplosionForce(500f, transform.position, 12f);
        }
    }
}
