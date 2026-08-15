using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class MusicSetupTool
{
    static MusicSetupTool()
    {
        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SessionState.GetBool("MusicSetupToolRun", false))
            return;

        SessionState.SetBool("MusicSetupToolRun", true);

        string[] scenes = new string[] 
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/BigIsland.unity",
            "Assets/Scenes/Village.unity"
        };

        string mainMenuClipPath = "Assets/Assets/Music/LOOP VERSIONS/04 - Main Menu LOOP 85bpm.wav";
        string bigIslandClipPath = "Assets/Assets/Music/LOOP VERSIONS/02 - Intro Adventurous LOOP 142bpm.wav";
        string villageClipPath = "Assets/Assets/Music/LOOP VERSIONS/07 - Village Square LOOP 105bpm.wav";

        // Load the Audio Clips
        AudioClip mainMenuClip = AssetDatabase.LoadAssetAtPath<AudioClip>(mainMenuClipPath);
        AudioClip bigIslandClip = AssetDatabase.LoadAssetAtPath<AudioClip>(bigIslandClipPath);
        AudioClip villageClip = AssetDatabase.LoadAssetAtPath<AudioClip>(villageClipPath);

        if (mainMenuClip == null || bigIslandClip == null || villageClip == null)
        {
            Debug.LogError("[MusicSetupTool] Could not find one or more audio clips. Please check paths.");
            return;
        }

        // Save current scene state before changing
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Setup MainMenu
        if (SetupMainMenu(scenes[0], mainMenuClip))
            Debug.Log("✅ [MusicSetupTool] Successfully configured MainMenu music.");
        
        // 2. Setup BigIsland
        if (SetupGameplayScene(scenes[1], bigIslandClip, "BigIsland"))
            Debug.Log("✅ [MusicSetupTool] Successfully configured BigIsland music.");

        // 3. Setup Village
        if (SetupGameplayScene(scenes[2], villageClip, "Village"))
            Debug.Log("✅ [MusicSetupTool] Successfully configured Village music.");

        Debug.Log("🎉 [MusicSetupTool] All scene music automatically configured!");
    }

    private static bool SetupMainMenu(string scenePath, AudioClip clip)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return false;

        bool changed = false;
        var musicManagerObj = GameObject.Find("MusicManager");
        if (musicManagerObj != null)
        {
            var manager = musicManagerObj.GetComponent("MusicManager");
            if (manager != null)
            {
                SerializedObject so = new SerializedObject(manager);
                so.FindProperty("initialMusic").objectReferenceValue = clip;
                so.ApplyModifiedProperties();
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.SaveScene(scene);
            return true;
        }
        return false;
    }

    private static bool SetupGameplayScene(string scenePath, AudioClip clip, string sceneName)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return false;

        bool changed = false;

        // Remove old background music managers
        var oldManager = GameObject.Find("BackgroundMusicManager");
        if (oldManager != null)
        {
            Object.DestroyImmediate(oldManager);
            changed = true;
        }

        // Check if SceneMusicSetter already exists
        var setterObj = GameObject.Find("SceneMusic");
        if (setterObj == null)
        {
            setterObj = new GameObject("SceneMusic");
            changed = true;
        }

        var setterComponent = setterObj.GetComponent("SceneMusicSetter");
        if (setterComponent == null)
        {
            // Adding component via Reflection since we might not have reference to it if it's in a different assembly
            var type = System.Type.GetType("SceneMusicSetter, Assembly-CSharp");
            if (type != null)
            {
                setterComponent = setterObj.AddComponent(type);
                changed = true;
            }
        }

        if (setterComponent != null)
        {
            SerializedObject so = new SerializedObject(setterComponent);
            so.FindProperty("sceneMusic").objectReferenceValue = clip;
            so.ApplyModifiedProperties();
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        return false;
    }
}
