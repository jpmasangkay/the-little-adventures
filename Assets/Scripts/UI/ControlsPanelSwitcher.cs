using UnityEngine;
using UnityEngine.UI;

public class ControlsPanelSwitcher : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    public Button openControlsButton;
    public Button closeControlsButton;

    private void Start()
    {
        // Auto-find missing references just in case they were left blank in the Inspector
        if (controlsPanel == null)
        {
            // Find the Keybind Reset Button, which is always inside the Controls Panel
            var resetBtn = Resources.FindObjectsOfTypeAll<KeybindResetButton>();
            if (resetBtn.Length > 0 && resetBtn[0].gameObject.scene.IsValid())
            {
                Transform t = resetBtn[0].transform;
                // Walk up the hierarchy to find the main panel (usually named ControlsPanel or Keybinds)
                while (t != null && t.GetComponent<Canvas>() == null)
                {
                    if (t.name.ToLower().Contains("control") || t.name.ToLower().Contains("keybind") || t.name.ToLower().Contains("panel"))
                    {
                        controlsPanel = t.gameObject;
                    }
                    t = t.parent;
                }
                // Fallback if name matching failed
                if (controlsPanel == null) controlsPanel = resetBtn[0].transform.parent.gameObject;
            }
        }

        if (settingsPanel == null)
        {
            var volumeSlider = Resources.FindObjectsOfTypeAll<Slider>();
            foreach (var slider in volumeSlider)
            {
                if (slider.gameObject.name == "VolumeSlider" && slider.gameObject.scene.IsValid())
                {
                    Transform t = slider.transform;
                    while (t != null && t.GetComponent<Canvas>() == null)
                    {
                        if (t.name.ToLower().Contains("setting") || t.name.ToLower().Contains("panel"))
                        {
                            settingsPanel = t.gameObject;
                        }
                        t = t.parent;
                    }
                }
            }
        }
        if (openControlsButton != null)
        {
            openControlsButton.onClick.AddListener(() =>
            {
                if (settingsPanel != null) settingsPanel.SetActive(false);
                if (controlsPanel != null) controlsPanel.SetActive(true);
            });
        }

        if (closeControlsButton != null)
        {
            closeControlsButton.onClick.AddListener(() =>
            {
                if (controlsPanel != null) controlsPanel.SetActive(false);
                if (settingsPanel != null) settingsPanel.SetActive(true);
            });
        }

        // Default state: Settings open, Controls closed
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }
}
