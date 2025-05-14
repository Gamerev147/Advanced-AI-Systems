using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AI_Agent))]
public class PatrolPathEditor : Editor
{
    private AI_Agent m_Agent;
    private Vector3 m_LastPosition;

    private bool _toggleGizmos = true;
    private bool _showAppearance = false;

    private Color _handleColor = Color.green;
    private Color _lineColor = Color.green;
    private Color _labelColor = Color.white;
    private float _lineThickness = 0.1f;
    private float _handleSize = 0.25f;

    private void OnEnable()
    {
        m_Agent = (AI_Agent)target;
        m_LastPosition = m_Agent.transform.position;

        // Load saved appearance settings
        if (EditorPrefs.HasKey($"{m_Agent.GetInstanceID()}_HandleColor"))
        {
            _handleColor = EditorPrefsX.GetColor($"{m_Agent.GetInstanceID()}_HandleColor");
        }

        if (EditorPrefs.HasKey($"{m_Agent.GetInstanceID()}_LineColor"))
        {
            _lineColor = EditorPrefsX.GetColor($"{m_Agent.GetInstanceID()}_LineColor");
        }

        if (EditorPrefs.HasKey($"{m_Agent.GetInstanceID()}_LabelColor"))
        {
            _labelColor = EditorPrefsX.GetColor($"{m_Agent.GetInstanceID()}_LabelColor");
        }

        if (EditorPrefs.HasKey($"{m_Agent.GetInstanceID()}_LineThickness"))
        {
            _lineThickness = EditorPrefs.GetFloat($"{m_Agent.GetInstanceID()}_LineThickness");
        }

        if (EditorPrefs.HasKey($"{m_Agent.GetInstanceID()}_HandleSize"))
        {
            _handleSize = EditorPrefs.GetFloat($"{m_Agent.GetInstanceID()}_HandleSize");
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Patrol Point Editor", EditorStyles.boldLabel);

        // Foldout for appearance settings
        _showAppearance = EditorGUILayout.Foldout(_showAppearance, "Appearance Settings");
        if (_showAppearance)
        {
            EditorGUI.BeginChangeCheck();
            _handleColor = EditorGUILayout.ColorField("Handle Color", _handleColor);
            _lineColor = EditorGUILayout.ColorField("Line Color", _lineColor);
            _labelColor = EditorGUILayout.ColorField("Label Color", _labelColor);
            _lineThickness = EditorGUILayout.Slider("Line Thickness", _lineThickness, 1f, 6f);
            _handleSize = EditorGUILayout.Slider("Handle Size", _handleSize, 0.1f, 2f);

            // Save the appearance settings after changes
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefsX.SetColor($"{m_Agent.GetInstanceID()}_HandleColor", _handleColor);
                EditorPrefsX.SetColor($"{m_Agent.GetInstanceID()}_LineColor", _lineColor);
                EditorPrefsX.SetColor($"{m_Agent.GetInstanceID()}_LabelColor", _labelColor);
                EditorPrefs.SetFloat($"{m_Agent.GetInstanceID()}_LineThickness", _lineThickness);
                EditorPrefs.SetFloat($"{m_Agent.GetInstanceID()}_HandleSize", _handleSize);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space(20f);
        EditorGUILayout.LabelField("Patrol Point Settings");

        // Button to create patrol point in front of entity
        if (GUILayout.Button("Add Patrol Point"))
        {
            Undo.RecordObject(m_Agent, "Add Patrol Point");

            // Add a new point 2 units in front of the entity
            Vector3 defaultPosition = m_Agent.transform.position + m_Agent.transform.forward * 2f;
            PatrolPoint newPoint = new PatrolPoint
            {
                Position = defaultPosition
            };
            m_Agent.PatrolPoints.Add(newPoint);

            EditorUtility.SetDirty(m_Agent);
        }

        // Button to save patrol points preset
        if (GUILayout.Button("Save Patrol Preset"))
        {
            SavePatrolPreset();
        }

        // Button to load a patrol preset
        if (GUILayout.Button("Load Patrol Preset"))
        {
            LoadPatrolPreset();
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
        Vector3 currentPos = m_Agent.transform.position;
        Vector3 delta = currentPos - m_LastPosition;

        // Detect when the root game object is moved
        // Switch the coordinate space to local while moving, and world space when not moving
        if (!Application.isPlaying && delta != Vector3.zero)
        {
            for (int i = 0; i < m_Agent.PatrolPoints.Count; i++)
            {
                m_Agent.PatrolPoints[i].Position += delta;
            }

            EditorUtility.SetDirty(m_Agent);
        }

        m_LastPosition = currentPos;

        // Point editing
        for (int i = 0; i < m_Agent.PatrolPoints.Count; i++)
        {
            PatrolPoint point = m_Agent.PatrolPoints[i];
            Vector3 worldPos = point.Position;

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Agent, "Move Waypoint");
                point.Position = newWorldPos;
                EditorUtility.SetDirty(m_Agent);
            }

            // Draw point handles and lables
            Handles.color = _handleColor;
            if (_toggleGizmos) Handles.SphereHandleCap(0, worldPos, Quaternion.identity, _handleSize, EventType.Repaint);
            
            GUIStyle style = new GUIStyle();
            style.normal.textColor = _labelColor;
            if (_toggleGizmos) Handles.Label(worldPos + Vector3.up * 0.25f, $"Waypoint {i}", style);

            // Draw point connection lines
            Handles.color = _lineColor;
            if (i < m_Agent.PatrolPoints.Count - 1)
            {
                Vector3 nextPos = m_Agent.PatrolPoints[i + 1].Position;
                if (_toggleGizmos) Handles.DrawLine(worldPos, nextPos, _lineThickness);
            }

            // Draw loop connection
            if (m_Agent.LoopPatrol && m_Agent.PatrolPoints.Count > 2)
            {
                if (_toggleGizmos) Handles.DrawLine(m_Agent.PatrolPoints[m_Agent.PatrolPoints.Count - 1].Position, m_Agent.PatrolPoints[0].Position, _lineThickness);
            }
        }
    }

    private void SavePatrolPreset()
    {
        // Get the save file path
        string path = EditorUtility.SaveFilePanelInProject("Save Patrol Path", "NewPatrolPath", "asset", "Please enter a file name:");
        if (string.IsNullOrEmpty(path)) return;

        // Create a new SO containing the patrol points
        AISO_PatrolPath newPreset = ScriptableObject.CreateInstance<AISO_PatrolPath>();
        foreach (var point in m_Agent.PatrolPoints)
        {
            // Save the point in local space, relative to the entity
            Vector3 localPos = m_Agent.transform.InverseTransformPoint(point.Position);
            newPreset.patrolPoints.Add(new PatrolPoint
            {
                Position = localPos,
                SkipIdle = point.SkipIdle
            });
        }

        AssetDatabase.CreateAsset(newPreset, path);
        AssetDatabase.SaveAssets();

        Debug.LogWarning("Patrol path saved to {path}");
    }

    private void LoadPatrolPreset()
    {
        // Get patrol preset path
        string path = EditorUtility.OpenFilePanel("Load Patrol Preset", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;

        // Convert path
        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        // Load the patrol point preset
        AISO_PatrolPath loadedPreset = AssetDatabase.LoadAssetAtPath<AISO_PatrolPath>(path);
        if (loadedPreset == null)
        {
            Debug.LogError("Failed to load patrol preset.");
            return;
        }

        // Clear any existing waypoints
        if (EditorUtility.DisplayDialog("Clear All?", "This will delete any existing waypoints. Do you wish to continue?", "Yes", "No"))
        {
            m_Agent.PatrolPoints.Clear();
            EditorUtility.SetDirty(m_Agent);
        } else
        {
            return;
        }

        // Assign new patrol points
        foreach (var point in loadedPreset.patrolPoints)
        {
            // Convert the points back to world space
            Vector3 worldPos = m_Agent.transform.TransformPoint(point.Position);
            m_Agent.PatrolPoints.Add(new PatrolPoint
            {
                Position = worldPos,
                SkipIdle = point.SkipIdle
            });
        }

        EditorUtility.SetDirty(m_Agent);
        Debug.LogWarning("Loaded patrol path from preset.");
    }
}
