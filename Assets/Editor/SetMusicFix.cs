using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class SetMusicFix
{
    static SetMusicFix()
    {
        EditorApplication.delayCall += ApplyMusic;
    }

    private static void ApplyMusic()
    {
        if (SessionState.GetBool("SetMusicFixRun", false)) return;
        SessionState.SetBool("SetMusicFixRun", true);

        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        if (!scene.IsValid()) return;

        bool changed = false;

        string musicPath = "Assets/Assets/Music/LOOP VERSIONS/04 - Main Menu LOOP 85bpm.wav";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);

        if (clip != null)
        {
            // Try to find the existing AudioSource, usually on SceneMusic or Main Camera
            GameObject musicObj = GameObject.Find("SceneMusic");
            if (musicObj == null) musicObj = GameObject.Find("Main Camera");
            
            if (musicObj != null)
            {
                AudioSource audioSource = musicObj.GetComponent<AudioSource>();
                if (audioSource == null) audioSource = musicObj.AddComponent<AudioSource>();
                
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                
                changed = true;
            }
        }
        else
        {
            Debug.LogError("Could not find the Main Menu music file at: " + musicPath);
        }

        if (changed)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("🎉 [SetMusicFix] Successfully applied Main Menu loop music!");
        }
        
        // Close if it was additive and wasn't our main active scene
        if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
