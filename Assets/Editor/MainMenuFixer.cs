using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class MainMenuFixer
{
    static MainMenuFixer()
    {
        EditorApplication.delayCall += CleanMainMenu;
    }

    private static void CleanMainMenu()
    {
        // Ensure we only run this once
        if (SessionState.GetBool("MainMenuFixerRun", false))
            return;

        SessionState.SetBool("MainMenuFixerRun", true);

        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "MainMenu")
            return;

        bool changed = false;

        // 1. Clean Main Camera
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var tpc = mainCamera.GetComponent("ThirdPersonCamera");
            if (tpc != null)
            {
                Object.DestroyImmediate(tpc);
                changed = true;
                Debug.Log("✅ [MainMenuFixer] Removed ThirdPersonCamera from Main Camera.");
            }

            var optimizer = mainCamera.GetComponent("LightingOptimizer");
            if (optimizer != null)
            {
                Object.DestroyImmediate(optimizer);
                changed = true;
                Debug.Log("✅ [MainMenuFixer] Removed LightingOptimizer from Main Camera.");
            }
        }

        // 2. Remove redundant BackgroundMusicManager
        var bgmManager = GameObject.Find("BackgroundMusicManager");
        if (bgmManager != null)
        {
            Object.DestroyImmediate(bgmManager);
            changed = true;
            Debug.Log("✅ [MainMenuFixer] Removed redundant BackgroundMusicManager GameObject.");
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log("🎉 [MainMenuFixer] Cleaned and saved MainMenu scene successfully!");
        }
    }
}
