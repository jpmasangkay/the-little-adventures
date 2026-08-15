using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutoFixerMusic
{
    static AutoFixerMusic()
    {
        EditorApplication.delayCall += RunFixes;
    }

    private static void RunFixes()
    {
        // Only run once per editor session
        if (SessionState.GetBool("AutoFixerMusicRun", false))
            return;

        // Check if we already created it
        var existing = GameObject.Find("BackgroundMusicManager");
        if (existing == null)
        {
            GameObject bgm = new GameObject("BackgroundMusicManager");
            AudioSource audioSource = bgm.AddComponent<AudioSource>();
            
            // Load the specific track the user requested
            string clipPath = "Assets/Assets/Music/LOOP VERSIONS/02 - Intro Adventurous LOOP 142bpm.wav";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                audioSource.volume = 0.4f; // A reasonable default volume
                
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log("🎶 [AutoFixer] Successfully added BackgroundMusicManager to the scene and assigned the looping track.");
            }
            else
            {
                Debug.LogError("❌ [AutoFixer] Could not find the music clip at path: " + clipPath);
                Object.DestroyImmediate(bgm);
            }
        }
        
        SessionState.SetBool("AutoFixerMusicRun", true);
    }
}
