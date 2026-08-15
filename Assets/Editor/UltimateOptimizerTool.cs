using UnityEngine;
using UnityEditor;

public class UltimateOptimizerTool : EditorWindow
{
    [MenuItem("Tools/Run Ultimate Optimization Pass")]
    public static void ShowWindow()
    {
        GetWindow<UltimateOptimizerTool>("Ultimate Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ultimate Optimization Pass", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("This tool will perform a massive project-wide optimization pass on Draw Calls, Physics, and Animation.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Run Ultimate Optimization", GUILayout.Height(40)))
        {
            RunOptimizations();
        }
        GUI.backgroundColor = Color.white;
    }

    private void RunOptimizations()
    {
        // 1. GPU Instancing (Draw Calls)
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int materialCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            // Only modify if it doesn't already have instancing enabled
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                materialCount++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Optimization] Enabled GPU Instancing on {materialCount} materials! (Massive draw call reduction)");

        // 2. Physics Tick Reduction
        Time.fixedDeltaTime = 0.0333f; // 30 updates per second instead of 50
        Debug.Log("[Optimization] Lowered Physics Tick Rate to 30hz (40% CPU savings on physics)");

        // 3. Animation Culling (in current scene)
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Exclude);
        int animCount = 0;
        foreach (Animator anim in animators)
        {
            // Skip the player! We don't want to cull the player's animations
            if (anim.gameObject.CompareTag("Player") || anim.transform.root.CompareTag("Player"))
                continue;

            if (anim.cullingMode != AnimatorCullingMode.CullCompletely)
            {
                anim.cullingMode = AnimatorCullingMode.CullCompletely;
                EditorUtility.SetDirty(anim);
                animCount++;
            }
        }
        Debug.Log($"[Optimization] Configured {animCount} Animators in the scene to 'Cull Completely' when off-screen.");

        EditorUtility.DisplayDialog("Optimization Complete", "Successfully optimized Materials, Physics, and Animations!\n\nYour materials now use GPU Instancing, physics runs at 30hz, and invisible monsters stop animating.", "Awesome!");
    }
}
