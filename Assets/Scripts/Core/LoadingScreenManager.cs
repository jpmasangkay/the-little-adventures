using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main container for the loading screen (e.g. a Canvas or Panel)")]
    public GameObject loadingScreenCanvas;
    public Slider loadingBar;
    public TextMeshProUGUI progressText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(true);
        }

        if (loadingBar != null) loadingBar.value = 0f;
        if (progressText != null) progressText.text = "LOADING... 0%";

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;
        while (fakeProgress < 1f || operation.progress < 0.9f)
        {
            // Clamp deltaTime to prevent huge frame-drop spikes from instantly filling the bar
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.1f);
            fakeProgress += dt / 2.5f; // 2.5 seconds minimum load time
            float displayProgress = Mathf.Clamp01(Mathf.Min(fakeProgress, operation.progress / 0.9f));

            if (loadingBar != null)
            {
                loadingBar.value = displayProgress;
            }

            if (progressText != null)
            {
                progressText.text = "LOADING... " + (displayProgress * 100f).ToString("F0") + "%";
            }

            if (displayProgress >= 1f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Give it a tiny moment after activation before hiding the screen
        yield return new WaitForSecondsRealtime(0.1f);

        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(false);
        }
    }
}
