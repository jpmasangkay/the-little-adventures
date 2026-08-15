using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AutoFixer
{
    [MenuItem("Tools/Force Clean Healthbar")]
    public static void RunFixes()
    {
        if (Application.isPlaying) 
        {
            Debug.LogWarning("Cannot run AutoFixer in Play Mode. Stop the game first.");
            return;
        }

        if (SessionState.GetBool("AutoFixerRun_V3", false) && !Application.isEditor) return;
        SessionState.SetBool("AutoFixerRun_V3", true);

        // Ensure we are in BigIsland to fix the healthbar
        string scenePath = "Assets/Scenes/BigIsland.unity";
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string startingScene = EditorSceneManager.GetActiveScene().path;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return;

        bool changed = false;

        // Forcefully remove the broken Ilumisoft Healthbar script
        GameObject hbObj = GameObject.Find("Healthbar");
        if (hbObj != null)
        {
            var comps = hbObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var c in comps)
            {
                if (c != null && c.GetType().Name == "Healthbar")
                {
                    Object.DestroyImmediate(c);
                    changed = true;
                    Debug.Log("✅ [AutoFixer] Destroyed the broken Ilumisoft Healthbar script!");
                }
            }
        }

        if (changed)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("🎉 [AutoFixer] Cleaned up Big Island!");
        }

        // Return to where we were
        if (!string.IsNullOrEmpty(startingScene) && startingScene != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(startingScene, OpenSceneMode.Single);
        }
    }
}
