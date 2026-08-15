using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Editor tool that ensures the player character in BigIsland has an InputReader
/// component with the InputActionAsset correctly assigned.
///
/// Run via: Tools ▶ Fix BigIslandMC InputReader
/// </summary>
public class InputReaderFixer
{
    [MenuItem("Tools/Fix BigIslandMC InputReader")]
    public static void FixInputReader()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[InputReaderFixer] Stop Play Mode before running this tool.");
            return;
        }

        // ── 1. Save current scene & open BigIsland ────────────────────────────
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        string bigIslandPath = "Assets/Scenes/BigIsland.unity";
        var scene = EditorSceneManager.OpenScene(bigIslandPath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[InputReaderFixer] Could not open scene at '{bigIslandPath}'. " +
                           "Check the path matches your project.");
            return;
        }

        // ── 2. Locate the Input Action Asset ─────────────────────────────────
        string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
        InputActionAsset actionAsset = null;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            actionAsset  = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (actionAsset != null)
            {
                Debug.Log($"[InputReaderFixer] Using InputActionAsset: {path}");
                break;
            }
        }

        if (actionAsset == null)
        {
            Debug.LogError("[InputReaderFixer] No InputActionAsset found in the project! " +
                           "Create one via: Assets ▶ Create ▶ Input Actions.");
            return;
        }

        // ── 3. Find the player GameObject ─────────────────────────────────────
        // Look for "BigIslandMC" first, then fall back to any object with PlayerMovement.
        GameObject playerGO = GameObject.Find("BigIslandMC");
        if (playerGO == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerGO = pm.gameObject;
        }

        if (playerGO == null)
        {
            Debug.LogError("[InputReaderFixer] Could not find 'BigIslandMC' or any object " +
                           "with PlayerMovement in the BigIsland scene.");
            return;
        }

        Debug.Log($"[InputReaderFixer] Found player: '{playerGO.name}'");

        bool sceneModified = false;

        // ── 4. Add InputReader if missing ─────────────────────────────────────
        InputReader reader = playerGO.GetComponent<InputReader>();
        if (reader == null)
        {
            reader = playerGO.AddComponent<InputReader>();
            Debug.Log("[InputReaderFixer] ✅ Added InputReader component.");
            sceneModified = true;
        }
        else
        {
            Debug.Log("[InputReaderFixer] InputReader already present.");
        }

        // ── 5. Wire the InputActionAsset if not already set ───────────────────
        SerializedObject so    = new SerializedObject(reader);
        SerializedProperty prop = so.FindProperty("inputActions");

        if (prop != null && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = actionAsset;
            so.ApplyModifiedProperties();
            Debug.Log("[InputReaderFixer] ✅ Assigned InputActionAsset to InputReader.");
            sceneModified = true;
        }
        else if (prop != null && prop.objectReferenceValue != null)
        {
            Debug.Log($"[InputReaderFixer] InputActionAsset already assigned: " +
                      $"{prop.objectReferenceValue.name}");
        }

        // ── 6. Also ensure PlayerCombat has InputReader (it uses [RequireComponent]) ──
        PlayerCombat combat = playerGO.GetComponent<PlayerCombat>();
        if (combat != null)
        {
            // InputReader is already there; nothing extra needed.
            Debug.Log("[InputReaderFixer] PlayerCombat is present and shares the InputReader.");
        }

        // ── 7. Save ───────────────────────────────────────────────────────────
        if (sceneModified)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[InputReaderFixer] 🎉 BigIsland scene saved successfully.");
        }
        else
        {
            Debug.Log("[InputReaderFixer] No changes were needed — scene is already correct.");
        }

        // ── 8. Return to original scene ───────────────────────────────────────
        if (!string.IsNullOrEmpty(originalScenePath) &&
            originalScenePath != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }
}
