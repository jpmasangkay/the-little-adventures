using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for EnemySpawner.
/// Adds an "Add Spawn Zone" button that auto-creates a child GameObject,
/// registers it in the spawnZones array, and selects it so you can
/// immediately drag it to the right position in the Scene view.
/// </summary>
[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    // Distinct colours cycled through as new zones are added
    private static readonly Color[] ZoneColors =
    {
        new Color(0.0f, 1.0f, 0.8f, 0.25f),  // cyan
        new Color(1.0f, 0.6f, 0.0f, 0.25f),  // orange
        new Color(0.8f, 0.0f, 1.0f, 0.25f),  // purple
        new Color(1.0f, 0.2f, 0.4f, 0.25f),  // pink
        new Color(0.2f, 1.0f, 0.2f, 0.25f),  // green
        new Color(1.0f, 1.0f, 0.0f, 0.25f),  // yellow
    };

    public override void OnInspectorGUI()
    {
        EnemySpawner spawner = (EnemySpawner)target;

        // ── Add Spawn Zone button ─────────────────────────────────────────────
        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button("＋  Add Spawn Zone", GUILayout.Height(32)))
        {
            AddSpawnZone(spawner);
        }
        GUI.backgroundColor = Color.white;

        // ── Remove last zone button ───────────────────────────────────────────
        bool hasZones = spawner.spawnZones != null && spawner.spawnZones.Length > 0;
        EditorGUI.BeginDisabledGroup(!hasZones);
        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("－  Remove Last Spawn Zone", GUILayout.Height(24)))
        {
            RemoveLastSpawnZone(spawner);
        }
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(6);

        // ── Help box ─────────────────────────────────────────────────────────
        if (!hasZones)
        {
            EditorGUILayout.HelpBox(
                "No spawn zones yet.\n" +
                "Click \"Add Spawn Zone\" to create one, then drag it to an open area in the Scene view.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"{spawner.spawnZones.Length} zone(s) active.  " +
                "Select a zone's GameObject in the Hierarchy and move it in the Scene view to reposition it.",
                MessageType.None);
        }

        EditorGUILayout.Space(4);

        // ── Default inspector fields ──────────────────────────────────────────
        DrawDefaultInspector();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddSpawnZone(EnemySpawner spawner)
    {
        // Determine next index for naming and colouring
        int index = spawner.spawnZones == null ? 0 : spawner.spawnZones.Length;

        // Find or create a dedicated "SpawnZones" root so zones are INDEPENDENT
        // of the EnemySpawner. Parenting to EnemySpawner caused all zones to move
        // together when the spawner was moved.
        GameObject zonesRoot = GameObject.Find("SpawnZones");
        if (zonesRoot == null)
        {
            zonesRoot = new GameObject("SpawnZones");
            Undo.RegisterCreatedObjectUndo(zonesRoot, "Create SpawnZones Root");
        }

        // Create the zone child under the shared root
        GameObject zoneGO = new GameObject($"SpawnZone_{index + 1}");
        Undo.RegisterCreatedObjectUndo(zoneGO, "Create Spawn Zone");
        zoneGO.transform.SetParent(zonesRoot.transform, worldPositionStays: false);

        // Place it at the spawner's world position so it starts nearby
        zoneGO.transform.position = spawner.transform.position;

        // Use SerializedObject so Unity properly tracks the reference
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty zonesProp = so.FindProperty("spawnZones");

        zonesProp.arraySize = index + 1;
        SerializedProperty elem       = zonesProp.GetArrayElementAtIndex(index);
        SerializedProperty centerProp = elem.FindPropertyRelative("center");
        SerializedProperty radiusProp = elem.FindPropertyRelative("radius");
        SerializedProperty colorProp  = elem.FindPropertyRelative("gizmoColor");

        centerProp.objectReferenceValue = zoneGO.transform;
        radiusProp.floatValue           = 10f;
        colorProp.colorValue            = ZoneColors[index % ZoneColors.Length];

        so.ApplyModifiedProperties();

        // Select the zone so the user can drag it to the right spot immediately
        Selection.activeGameObject = zoneGO;

        Debug.Log($"[EnemySpawner] Created SpawnZone_{index + 1} under 'SpawnZones'. " +
                  "Drag it to an open area in the Scene view.");
    }

    private void RemoveLastSpawnZone(EnemySpawner spawner)
    {
        if (spawner.spawnZones == null || spawner.spawnZones.Length == 0) return;

        Undo.RecordObject(spawner, "Remove Spawn Zone");

        int lastIndex = spawner.spawnZones.Length - 1;
        EnemySpawner.SpawnZone last = spawner.spawnZones[lastIndex];

        // Destroy the associated GameObject if it's a child of the spawner
        if (last?.center != null && last.center.parent == spawner.transform)
            Undo.DestroyObjectImmediate(last.center.gameObject);

        // Shrink array
        EnemySpawner.SpawnZone[] updated = new EnemySpawner.SpawnZone[lastIndex];
        System.Array.Copy(spawner.spawnZones, updated, lastIndex);
        spawner.spawnZones = updated;

        EditorUtility.SetDirty(spawner);
    }
}
