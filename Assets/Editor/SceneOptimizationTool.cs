using UnityEngine;
using UnityEditor;

public class SceneOptimizationTool : EditorWindow
{
    [MenuItem("Tools/Optimize Scene Rendering (Occlusion Culling)")]
    public static void ShowWindow()
    {
        GetWindow<SceneOptimizationTool>("Optimize Scene");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scene Rendering Optimizer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("This tool will completely optimize the rendering of the current scene.", EditorStyles.wordWrappedLabel);
        GUILayout.Label("It automatically finds all non-moving environment objects (trees, houses, rocks) and configures them so they don't render when hidden behind mountains or buildings.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Optimize & Bake Occlusion Culling", GUILayout.Height(40)))
        {
            OptimizeScene();
        }
        GUI.backgroundColor = Color.white;
    }

    private void OptimizeScene()
    {
        MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Exclude);
        
        int markedCount = 0;

        // Process all Mesh Renderers
        foreach (MeshRenderer renderer in allRenderers)
        {
            GameObject go = renderer.gameObject;
            
            // Skip objects that move or are characters!
            if (go.GetComponentInParent<Rigidbody>() != null) continue;
            if (go.GetComponentInParent<CharacterController>() != null) continue;
            if (go.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) continue;
            if (go.GetComponentInParent<Animator>() != null) continue;
            if (go.CompareTag("Player")) continue;

            // Mark as static for culling and batching
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            flags |= StaticEditorFlags.OccluderStatic;
            flags |= StaticEditorFlags.OccludeeStatic;
            flags |= StaticEditorFlags.BatchingStatic; // Huge draw call optimization

            GameObjectUtility.SetStaticEditorFlags(go, flags);
            markedCount++;
        }

        // Process Terrains (they are usually massive occluders)
        foreach (Terrain t in terrains)
        {
            GameObject go = t.gameObject;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            flags |= StaticEditorFlags.OccluderStatic;
            flags |= StaticEditorFlags.OccludeeStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            markedCount++;
        }

        Debug.Log($"Successfully marked {markedCount} objects as static environment for Occlusion Culling.");
        Debug.Log("Starting Occlusion Culling Bake. This may take a minute or two... check the progress bar in the bottom right!");

        // This triggers the actual occlusion calculations
        StaticOcclusionCulling.Compute();
        
        Debug.Log("Occlusion Culling Bake Complete! The scene should now render significantly faster.");
    }
}
