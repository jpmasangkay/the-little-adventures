using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Buttons (Auto-Found if null)")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Transition")]
    public CanvasGroup fadePanel;

    private void FixNonUniformScale(Transform t)
    {
        if (t == null) return;
        
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            float scaleX = rt.localScale.x;
            float scaleY = rt.localScale.y;
            if (Mathf.Abs(scaleX - scaleY) > 0.01f && scaleY != 0)
            {
                float ratio = scaleX / scaleY;
                rt.localScale = new Vector3(scaleY, scaleY, rt.localScale.z);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * ratio, rt.sizeDelta.y);
            }
        }

        foreach (Transform child in t)
        {
            FixNonUniformScale(child);
        }
    }

    void Start()
    {
        // RUNTIME FIX: Fix non-uniform scales recursively
        if (mainPanel != null) FixNonUniformScale(mainPanel.transform);
        if (settingsPanel != null) FixNonUniformScale(settingsPanel.transform);

        // RUNTIME FIX: Fix Canvas Scaler stretching
        if (mainPanel != null)
        {
            UnityEngine.UI.CanvasScaler scaler = mainPanel.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null) scaler.matchWidthOrHeight = 0.5f;
        }

        // Ensure Main Panel is on and Settings is off initially
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // Ensure time is flowing (in case we quit from a paused state previously)
        Time.timeScale = 1f;

        // Auto-wire buttons
        if (playButton == null) playButton = FindButton("PlayButton");
        if (settingsButton == null) settingsButton = FindButton("SettingsButton");
        if (quitButton == null) quitButton = FindButton("QuitButton");

        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // Fix Close Button on Settings Panel
        if (settingsPanel != null)
        {
            Transform closeBtn = settingsPanel.transform.Find("Close");
            if (closeBtn != null)
            {
                // Remove broken Close component if it exists
                Component badClose = closeBtn.GetComponent("CartoonUI.Close");
                if (badClose != null) Destroy(badClose);

                // Try iterating if not found
                foreach (var comp in closeBtn.GetComponents<MonoBehaviour>())
                {
                    if (comp != null && comp.GetType().Name == "Close")
                    {
                        Destroy(comp);
                    }
                }

                Button cb = closeBtn.GetComponent<Button>();
                if (cb != null) cb.onClick.AddListener(CloseSettings);
            }
        }
    }

    private IEnumerator TransitionToGame()
    {
        // Disable buttons so user can't spam them during transition
        if (playButton != null) playButton.interactable = false;
        if (settingsButton != null) settingsButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.FadeOut(2f);
        }

        // 1. Fade the screen to black visually
        float timer = 0f;
        while (timer < 2f)
        {
            timer += Time.unscaledDeltaTime;
            if (fadePanel != null)
            {
                fadePanel.alpha = timer / 2f;
            }
            yield return null;
        }

        // 2. Load the scene via our Global Manager
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene("Village");
        }
        else
        {
            SceneManager.LoadScene("Village");
            Debug.LogWarning("[MainMenuController] LoadingScreenManager not found, falling back to direct load.");
        }
    }

    private Button FindButton(string btnName)
    {
        // Search children of main panel
        if (mainPanel != null)
        {
            Button[] allButtons = mainPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.gameObject.name == btnName) return b;
            }
        }
        
        // Search children of settings panel
        if (settingsPanel != null)
        {
            Button[] allButtons = settingsPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.gameObject.name == btnName) return b;
            }
        }

        // Fallback global search for inactive objects
        Button[] anyButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var b in anyButtons)
        {
            if (b.gameObject.name == btnName && b.gameObject.scene.IsValid()) return b;
        }

        return null;
    }

    public void PlayGame()
    {
        Debug.Log("Play Game triggered!");
        
        StartCoroutine(TransitionToGame());
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game triggered!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
