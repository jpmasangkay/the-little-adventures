using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class BuildSettingsFixer
{
    static BuildSettingsFixer()
    {
        EditorApplication.delayCall += RunFix;
    }

    private static void RunFix()
    {
        if (SessionState.GetBool("BuildSettingsFixerRun", false))
            return;
        SessionState.SetBool("BuildSettingsFixerRun", true);

        string[] requiredScenes = {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Village.unity",
            "Assets/Scenes/BigIsland.unity"
        };

        var currentScenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;

        foreach (string scenePath in requiredScenes)
        {
            // Only add the scene if it's not already in the build settings
            if (!currentScenes.Any(s => s.path == scenePath))
            {
                currentScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                changed = true;
                UnityEngine.Debug.Log($"✅ [BuildSettingsFixer] Added {scenePath} to Build Settings.");
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = currentScenes.ToArray();
            UnityEngine.Debug.Log("🎉 [BuildSettingsFixer] Successfully added missing scenes to the Build Settings! They will now load properly.");
        }
    }
}
