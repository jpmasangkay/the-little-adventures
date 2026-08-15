using UnityEngine;

public class SceneMusicSetter : MonoBehaviour
{
    [Tooltip("The music track that should play when this scene loads.")]
    public AudioClip sceneMusic;

    [Tooltip("How long the crossfade should take in seconds.")]
    public float crossfadeDuration = 1.0f;

    [Tooltip("The target volume for the music.")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    void Start()
    {
        // Tell the persistent MusicManager to crossfade to this scene's track
        if (MusicManager.Instance != null && sceneMusic != null)
        {
            MusicManager.Instance.CrossfadeTo(sceneMusic, crossfadeDuration, volume);
        }
        else if (MusicManager.Instance == null)
        {
            Debug.LogWarning("SceneMusicSetter tried to play music, but no MusicManager was found in the scene! Make sure you start from the MainMenu or place a MusicManager in this scene for testing.");
        }
    }
}
