#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Places a fully-configured EnemySpawner in the active (BigIsland) scene
/// and ensures the SlimePolyart + TurtleShellPolyart prefabs have the
/// required EnemyAI and EnemyHealth components.
///
/// Run from:  Tools ▸ RPG ▸ Setup BigIsland Enemy Spawner
/// </summary>
public class BigIslandEnemySetup
{
    private const string SlimePath       = "Assets/Assets/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/SlimePolyart.prefab";
    private const string TurtlePath      = "Assets/Assets/RPG Monster DUO PBR Polyart/Prefabs/PolyartDefault/TurtleShellPolyart.prefab";
    private const string SpawnerObjName  = "EnemySpawner";

    [MenuItem("Tools/RPG/Setup BigIsland Enemy Spawner")]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemySetup] Stop Play Mode first.");
            return;
        }

        // ── Load prefab assets ────────────────────────────────────────────────
        GameObject slimePrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePath);
        GameObject turtlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePath);

        if (slimePrefab == null)
        {
            Debug.LogError($"[EnemySetup] SlimePolyart prefab not found at: {SlimePath}");
            return;
        }
        if (turtlePrefab == null)
        {
            Debug.LogError($"[EnemySetup] TurtleShellPolyart prefab not found at: {TurtlePath}");
            return;
        }

        Debug.Log("[EnemySetup] Found both enemy prefabs.");

        // ── Patch prefabs so they have EnemyAI + EnemyHealth ─────────────────
        PatchEnemyPrefab(slimePrefab,  maxHealth: 30f, xpReward: 25,  attackDamage: 5f);
        PatchEnemyPrefab(turtlePrefab, maxHealth: 60f, xpReward: 50,  attackDamage: 10f);

        // ── Place / update EnemySpawner in the scene ──────────────────────────
        GameObject spawnerObj = GameObject.Find(SpawnerObjName);
        if (spawnerObj != null)
        {
            Debug.Log("[EnemySetup] Found existing EnemySpawner — updating it.");
        }
        else
        {
            spawnerObj = new GameObject(SpawnerObjName);
            // Position it roughly in the centre of the island at a safe height
            spawnerObj.transform.position = new Vector3(0f, 5f, 0f);
            Debug.Log("[EnemySetup] Created new EnemySpawner GameObject.");
        }

        EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
        if (spawner == null)
            spawner = spawnerObj.AddComponent<EnemySpawner>();

        // ── Configure the spawner via SerializedObject so Undo works ─────────
        SerializedObject so = new SerializedObject(spawner);

        // Spawn radius + counts
        so.FindProperty("spawnRadius").floatValue      = 30f;
        so.FindProperty("raycastHeight").floatValue    = 60f;
        so.FindProperty("maxLiveEnemies").intValue     = 8;
        so.FindProperty("initialSpawnCount").intValue  = 5;
        so.FindProperty("respawnInterval").floatValue  = 15f;

        // Ground mask — Default layer (1) is almost always the terrain
        so.FindProperty("groundMask").intValue = ~0; // all layers — safest default

        // Enemies array: Slime (weight 3) + Turtle (weight 1)
        SerializedProperty enemiesArr = so.FindProperty("enemies");
        enemiesArr.arraySize = 2;

        SerializedProperty slimeEntry = enemiesArr.GetArrayElementAtIndex(0);
        slimeEntry.FindPropertyRelative("prefab").objectReferenceValue = slimePrefab;
        slimeEntry.FindPropertyRelative("weight").intValue = 3;

        SerializedProperty turtleEntry = enemiesArr.GetArrayElementAtIndex(1);
        turtleEntry.FindPropertyRelative("prefab").objectReferenceValue = turtlePrefab;
        turtleEntry.FindPropertyRelative("weight").intValue = 1;

        so.ApplyModifiedProperties();

        // ── Mark scene dirty and save ─────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Undo.RegisterCreatedObjectUndo(spawnerObj, "Setup Enemy Spawner");

        Debug.Log("🎉 [EnemySetup] Done!\n" +
                  $"  • EnemySpawner placed at {spawnerObj.transform.position}\n" +
                  "  • 5 enemies will spawn on Play (3x Slime, 1x Turtle weight ratio)\n" +
                  "  • Up to 8 live at once, respawning every 15 s\n\n" +
                  "⚠  If enemies still don't appear, move the EnemySpawner GameObject\n" +
                  "   so it is centred over your terrain in the Scene view, then press Play.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void PatchEnemyPrefab(GameObject prefab, float maxHealth, int xpReward, float attackDamage)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(
                   AssetDatabase.GetAssetPath(prefab)))
        {
            GameObject root = scope.prefabContentsRoot;

            // EnemyHealth
            EnemyHealth health = root.GetComponent<EnemyHealth>();
            if (health == null) health = root.AddComponent<EnemyHealth>();
            health.maxHealth = maxHealth;
            health.xpReward  = xpReward;

            // EnemyAI
            EnemyAI ai = root.GetComponent<EnemyAI>();
            if (ai == null) ai = root.AddComponent<EnemyAI>();
            ai.attackDamage = attackDamage;

            // Collider — needed for hit detection and physics
            Collider col = root.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
                cc.height = 1f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0, 0.5f, 0);
            }

            // Rigidbody — lets gravity keep the enemy on the ground (fixes floating).
            // Rotation is locked on X/Z so enemies don't tip over when bumped.
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 4f;          // high drag so they don't slide around
            rb.angularDamping = 10f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationZ;

            Debug.Log($"[EnemySetup] Patched '{root.name}' — Health:{maxHealth}, XP:{xpReward}, Dmg:{attackDamage}");
        }
    }
}
#endif
