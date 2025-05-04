using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ACC_ClimbPoint : MonoBehaviour
{
    [SerializeField] private List<NeighborPoint> neighbors;

    private void Awake()
    {
        var twoWayNeighbors = neighbors.Where(n => n.twoWayConnection);
        foreach (var neighbor in twoWayNeighbors)
        {
            neighbor.climbPoint?.CreatePointConnection(this, -neighbor.pointDirection, neighbor.connectionType, neighbor.twoWayConnection);
        }
    }

    public void CreatePointConnection(ACC_ClimbPoint climbPoint, Vector2 pointDir, ConnectionType connectionType, bool twoWay)
    {
        var neighbor = new NeighborPoint()
        {
            climbPoint = climbPoint,
            pointDirection = pointDir,
            connectionType = connectionType,
            twoWayConnection = twoWay
        };

        neighbors.Add(neighbor);
    }

    public NeighborPoint GetNeighbor(Vector2 climbDir)
    {
        NeighborPoint neighbor = null;

        if (climbDir.y != 0)
        {
            neighbor = neighbors.FirstOrDefault(n => n.pointDirection.y == climbDir.y);
        }

        if (climbDir.x != 0 && neighbor == null)
        {
            neighbor = neighbors.FirstOrDefault(n => n.pointDirection.x == climbDir.x);
        }

        return neighbor;
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(transform.position, transform.forward * 0.5f, Color.blue);

        foreach (var neighbor in neighbors)
        {
            if (neighbor.climbPoint != null)
            {
                Debug.DrawLine(transform.position, neighbor.climbPoint.transform.position, neighbor.twoWayConnection ? Color.green : Color.red);
            }
        }
    }
}

[System.Serializable]
public class NeighborPoint
{
    public ACC_ClimbPoint climbPoint;
    public Vector2 pointDirection;
    public ConnectionType connectionType;
    public bool twoWayConnection = true;
}

public enum ConnectionType
{
    Jump,
    Move
}
