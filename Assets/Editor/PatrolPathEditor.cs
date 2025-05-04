using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuardEnemy))]
public class PatrolPathEditor : Editor
{
    private GuardEnemy m_Enemy;
    private Vector3 m_LastPosition;

    private bool _toggleGizmos = true;

    private void OnEnable()
    {
        m_Enemy = (GuardEnemy)target;
        m_LastPosition = m_Enemy.transform.position;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Patrol Point Editor", EditorStyles.boldLabel);

        // Button to create patrol point in front of entity
        if (GUILayout.Button("Add Patrol Point"))
        {
            Undo.RecordObject(m_Enemy, "Add Patrol Point");

            // Add a new point 2 units in front of the entity
            Vector3 defaultPosition = m_Enemy.transform.position + m_Enemy.transform.forward * 2f;
            PatrolPoint newPoint = new PatrolPoint
            {
                Position = defaultPosition
            };
            m_Enemy.patrolPoints.Add(newPoint);

            EditorUtility.SetDirty(m_Enemy);
        }

        // Button to toggle the patrol point gizmos
        if (GUILayout.Button("Toggle Gizmos"))
        {
            _toggleGizmos = !_toggleGizmos;
        }

        EditorGUILayout.Space(25);
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        Vector3 currentPos = m_Enemy.transform.position;
        Vector3 delta = currentPos - m_LastPosition;

        // Detect when the root game object is moved
        // Switch the coordinate space to local while moving, and world space when not moving
        if (!Application.isPlaying && delta != Vector3.zero)
        {
            for (int i = 0; i < m_Enemy.patrolPoints.Count; i++)
            {
                m_Enemy.patrolPoints[i].Position += delta;
            }

            EditorUtility.SetDirty(m_Enemy);
        }

        m_LastPosition = currentPos;

        // Point editing
        for (int i = 0; i < m_Enemy.patrolPoints.Count; i++)
        {
            PatrolPoint point = m_Enemy.patrolPoints[i];
            Vector3 worldPos = point.Position;

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Enemy, "Move Waypoint");
                point.Position = newWorldPos;
                EditorUtility.SetDirty(m_Enemy);
            }

            if (_toggleGizmos) Handles.Label(worldPos + Vector3.up * 0.25f, $"Waypoint {i}");
        }
    }
}
