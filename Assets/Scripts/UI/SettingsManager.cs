using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Elements (Auto-Found)")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public Button closeButton;

    private T FindUIElement<T>(string name) where T : Component
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        foreach (var item in all)
        {
            if (item.gameObject.name == name && item.gameObject.scene.IsValid())
                return item;
        }
        return null;
    }

    void Start()
    {
        // Auto-find references if not set
        if (volumeSlider == null) 
        {
            volumeSlider = FindUIElement<Slider>("VolumeSlider");
            if (volumeSlider == null) volumeSlider = GetComponentInChildren<Slider>(true);
        }

        if (fullscreenToggle == null)
        {
            fullscreenToggle = FindUIElement<Toggle>("FullscreenToggle");
            if (fullscreenToggle == null) fullscreenToggle = GetComponentInChildren<Toggle>(true);
        }

        if (closeButton == null)
        {
            closeButton = FindUIElement<Button>("CloseButton");
        }

        // Load saved settings
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        // Apply them
        AudioListener.volume = savedVolume;
        Screen.fullScreen = isFullscreen;

        // Update UI
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (closeButton != null)
        {
            var menuController = Object.FindAnyObjectByType<MainMenuController>();
            if (menuController != null)
            {
                closeButton.onClick.AddListener(menuController.CloseSettings);
            }
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
