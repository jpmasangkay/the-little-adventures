using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool isPlayingA = true;

    public AudioClip initialMusic;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Try to find existing audio sources, or create them
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length >= 1) sourceA = sources[0];
            else sourceA = gameObject.AddComponent<AudioSource>();
            
            if (sources.Length >= 2) sourceB = sources[1];
            else sourceB = gameObject.AddComponent<AudioSource>();
            
            // Clean up any extra audio sources accidentally added
            for (int i = 2; i < sources.Length; i++)
            {
                Destroy(sources[i]);
            }
            
            sourceA.loop = true;
            sourceB.loop = true;
            sourceA.playOnAwake = false;
            sourceB.playOnAwake = false;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // If the newly loaded scene has no AudioListener, add one to the MusicManager so music keeps playing!
        if (FindAnyObjectByType<AudioListener>() == null)
        {
            if (GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }
    }

    void Start()
    {
        // If we have initial music and neither source is playing yet, play it!
        if (initialMusic != null && !sourceA.isPlaying && !sourceB.isPlaying)
        {
            PlayMusic(initialMusic);
        }
    }

    void Update()
    {
        // Unity doesn't re-run Awake() when scripts are hot-reloaded during Play mode.
        // We check every frame to ensure an ENABLED AudioListener exists somewhere.
        bool hasActiveListener = false;
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        foreach (var listener in listeners)
        {
            // FindObjectsOfType can return disabled components if their GameObject is active!
            if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
            {
                hasActiveListener = true;
                break;
            }
        }

        if (!hasActiveListener)
        {
            AudioListener al = GetComponent<AudioListener>();
            if (al == null)
            {
                al = gameObject.AddComponent<AudioListener>();
            }
            al.enabled = true;
        }
    }

    public void PlayMusic(AudioClip clip, float volume = 0.5f)
    {
        AudioSource activeSource = isPlayingA ? sourceA : sourceB;
        activeSource.clip = clip;
        activeSource.volume = volume;
        activeSource.Play();
    }

    public void CrossfadeTo(AudioClip nextClip, float fadeDuration = 1f, float targetVolume = 0.5f)
    {
        AudioSource activeSource = isPlayingA ? sourceA : sourceB;
        if (activeSource.clip == nextClip) return; // Already playing this track

        StartCoroutine(CrossfadeRoutine(nextClip, fadeDuration, targetVolume));
    }

    private IEnumerator CrossfadeRoutine(AudioClip nextClip, float fadeDuration, float targetVolume)
    {
        AudioSource activeSource = isPlayingA ? sourceA : sourceB;
        AudioSource nextSource = isPlayingA ? sourceB : sourceA;

        nextSource.clip = nextClip;
        nextSource.volume = 0f;
        nextSource.Play();

        float timer = 0f;
        float startVol = activeSource.volume;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            
            activeSource.volume = Mathf.Lerp(startVol, 0f, t);
            nextSource.volume = Mathf.Lerp(0f, targetVolume, t);
            
            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();
        nextSource.volume = targetVolume;
        
        isPlayingA = !isPlayingA;
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        AudioSource activeSource = isPlayingA ? sourceA : sourceB;
        float startVol = activeSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            // Use unscaled delta time so it still fades out if Time.timeScale == 0
            timer += Time.unscaledDeltaTime; 
            float t = timer / duration;
            activeSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();
    }
}
