using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGun : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform barrelTip;

    private Vector3 _dirToPlayer;

    private void Update()
    {
        _dirToPlayer = playerTarget.position - barrelTip.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(barrelTip.position, _dirToPlayer * 50f);
    }
}
