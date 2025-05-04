using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentChecker : MonoBehaviour
{
    [Header("Obstacle Detection")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] private float distance = 0.9f;
    [SerializeField] private float heightDistance = 6f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Climbing Detection")]
    [SerializeField] private float climbRayLength = 1.6f;
    [SerializeField] private Vector3 climbCheckOffset = Vector3.zero;
    [SerializeField] private int climbRayAmount = 12;
    [SerializeField] private LayerMask climbLayer;

    public ObstacleInfo CheckObstacle()
    {
        var hitData = new ObstacleInfo();

        //Raycast forward from feet
        Vector3 origin = transform.position + offset;
        hitData.hitFound = Physics.Raycast(origin, transform.forward, out hitData.hitInfo, distance, obstacleLayer);

        //If the feet hit an obstacle, raycast from that point vertically
        if (hitData.hitFound)
        {
            Vector3 heightOrigin = hitData.hitInfo.point + (Vector3.up * heightDistance);
            hitData.heightHitFound = Physics.Raycast(heightOrigin, Vector3.down, out hitData.heightInfo, heightDistance, obstacleLayer);

            Debug.DrawRay(heightOrigin, Vector3.down * heightDistance, Color.yellow);
        }

        return hitData;
    }

    public bool CheckClimbing(Vector3 climbDir, out RaycastHit climbInfo)
    {
        climbInfo = new RaycastHit();

        if (climbDir == Vector3.zero)
        {
            return false;
        }

        Vector3 climbOrigin = transform.position + Vector3.up * 1.5f;

        // Create multiple rays in the upward direction
        for (int i = 0; i < climbRayAmount; i++)
        {
            Debug.DrawRay(climbOrigin + climbCheckOffset * i, climbDir, Color.green, 0.5f);
            if (Physics.Raycast(climbOrigin + climbCheckOffset * i, climbDir, out RaycastHit hit, climbRayLength, climbLayer))
            {
                climbInfo = hit;
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + offset, transform.forward * distance);
    }
}

public struct ObstacleInfo
{
    public bool hitFound;
    public bool heightHitFound;
    public RaycastHit hitInfo;
    public RaycastHit heightInfo;
}
