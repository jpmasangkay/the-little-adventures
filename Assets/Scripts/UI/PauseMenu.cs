using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Pause menu — press Escape to pause / resume.
/// Requires a Canvas with at least a "Resume" and "Quit" button wired up in the Inspector.
///
/// Setup:
///  1. Create a Canvas (set to Screen-Space Overlay).
///  2. Add a semi-transparent panel, two buttons (Resume / Quit).
///  3. Assign the canvas root to `pauseCanvas`.
///  4. Wire OnResumeClicked() and OnQuitClicked() to the button OnClick events.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The root Canvas or Panel of the pause menu. Should be disabled by default.")]
    public GameObject pauseCanvas;

    [Header("Buttons (wire up OnClick in Inspector)")]
    public Button resumeButton;
    public Button quitButton;
    public Button mainMenuButton;

    [Header("Settings")]
    [Tooltip("Scene name of the main menu.")]
    public string mainMenuScene = "MainMenu";

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _isPaused = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (pauseCanvas  != null) pauseCanvas.SetActive(false);

        if (resumeButton    != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (quitButton      != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (mainMenuButton  != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused) Resume();
            else           Pause();
        }
    }

    // ── Public Button Callbacks ───────────────────────────────────────────────

    public void OnResumeClicked()  => Resume();
    public void OnQuitClicked()    => Application.Quit();
    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(mainMenuScene);
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
            Debug.LogWarning("[PauseMenu] LoadingScreenManager not found, falling back to direct load.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (pauseCanvas != null) pauseCanvas.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnDestroy()
    {
        // Ensure time scale is restored if the object is destroyed while paused
        Time.timeScale = 1f;
    }
}
