using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBarrel : MonoBehaviour
{
    [SerializeField] private GameObject brokenPrefab;
    [SerializeField] private GameObject explosionPrefab;

    public void Explode()
    {
        CameraController.Instance.ShakeCameraWobble(1.2f, 0.9f);

        Instantiate(brokenPrefab, transform.position, Quaternion.identity);
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
