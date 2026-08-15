using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ensures the "Portal" tag exists in TagManager and wires up the PortaltoBigIsland
/// GameObject in the Village scene with a ScenePortal component and loading screen.
/// </summary>
[InitializeOnLoad]
public class CustomPortalSetup
{
    static CustomPortalSetup()
    {
        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (SessionState.GetBool("CustomPortalSetupRun", false)) return;
        SessionState.SetBool("CustomPortalSetupRun", true);
        
        string scenePath = "Assets/Scenes/Village.unity";
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        if (!scene.IsValid()) return;
        
        bool changed = false;

        // ── Ensure the "Portal" tag exists in the project ────────────────────────
        EnsureTagExists("Portal");

        GameObject portalObj = GameObject.Find("PortaltoBigIsland");
        if (portalObj != null)
        {
            // Apply the Portal tag so ScenePortal.CompareTag(portalTag) works at runtime
            if (portalObj.tag != "Portal")
            {
                portalObj.tag = "Portal";
                Debug.Log("[CustomPortalSetup] Applied 'Portal' tag to PortaltoBigIsland.");
            }

            // Ensure collider
            Collider col = portalObj.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = portalObj.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(3, 4, 3);
                box.center = new Vector3(0, 2, 0);
            }
            else
            {
                if (col is MeshCollider mc) mc.convex = true;
                col.isTrigger = true;
            }

            // Add ScenePortal script
            var portalScript = portalObj.GetComponent("ScenePortal");
            if (portalScript == null)
            {
                var type = System.Type.GetType("ScenePortal, Assembly-CSharp");
                if (type != null) portalScript = portalObj.AddComponent(type);
            }

            // Wire up ScenePortal
            if (portalScript != null)
            {
                SerializedObject so = new SerializedObject(portalScript);
                so.FindProperty("targetSceneName").stringValue = "BigIsland";
                so.ApplyModifiedProperties();
            }


            changed = true;
            Debug.Log("✅ [CustomPortalSetup] Wired up PortaltoBigIsland with Loading Screen.");
        }

        if (changed)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("🎉 [CustomPortalSetup] Successfully configured custom portal in Village!");
        }
    }

    /// <summary>
    /// Adds <paramref name="tagName"/> to the project's TagManager if it is not already defined.
    /// Unity's <see cref="UnityEngine.GameObject.CompareTag"/> throws an exception for unregistered tags.
    /// </summary>
    private static void EnsureTagExists(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if the tag already exists
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                return; // Already registered — nothing to do
        }

        // Append the new tag
        int newIndex = tagsProp.arraySize;
        tagsProp.InsertArrayElementAtIndex(newIndex);
        tagsProp.GetArrayElementAtIndex(newIndex).stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        Debug.Log($"[CustomPortalSetup] Registered new tag: '{tagName}' in TagManager.");
    }
}
