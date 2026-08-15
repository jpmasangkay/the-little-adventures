using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance { get; private set; }

    [Header("UI overlay that blocks input while rebinding")]
    public GameObject listeningOverlay;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (listeningOverlay != null) listeningOverlay.SetActive(false);
    }

    /// <summary>
    /// Starts the interactive rebinding process for a specific action.
    /// </summary>
    public void StartRebinding(InputAction action, int bindingIndex, TextMeshProUGUI buttonText)
    {
        if (action == null) return;

        // Cancel any existing rebind operation
        _rebindOperation?.Cancel();

        // Disable the action temporarily to rebind it
        action.Disable();

        if (listeningOverlay != null) listeningOverlay.SetActive(true);
        if (buttonText != null) buttonText.text = "Listening...";

        // Begin the interactive rebind
        _rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // Exclude mouse movement from instantly stealing the bind
            .WithCancelingThrough("<Keyboard>/escape") // Pressing escape cancels the rebind
            .OnMatchWaitForAnother(0.1f) // Wait a tiny bit to ignore bouncy keys
            .OnComplete(operation => 
            {
                FinishRebind(action, bindingIndex, buttonText);
            })
            .OnCancel(operation => 
            {
                FinishRebind(action, bindingIndex, buttonText);
            })
            .Start();
    }

    private void FinishRebind(InputAction action, int bindingIndex, TextMeshProUGUI buttonText)
    {
        _rebindOperation.Dispose();
        _rebindOperation = null;

        if (listeningOverlay != null) listeningOverlay.SetActive(false);

        // Update the button text to the new key
        if (buttonText != null)
        {
            buttonText.text = InputControlPath.ToHumanReadableString(
                action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        action.Enable();

        // Save the new bindings globally
        SaveBindings(action.actionMap.asset);
    }

    /// <summary>
    /// Saves all binding overrides in the asset to PlayerPrefs as a JSON string.
    /// </summary>
    public static void SaveBindings(InputActionAsset asset)
    {
        if (asset == null) return;
        string rebinds = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
        PlayerPrefs.Save();
        Debug.Log("[KeybindManager] Bindings saved to PlayerPrefs.");
    }

    /// <summary>
    /// Loads binding overrides from PlayerPrefs and applies them to the asset.
    /// </summary>
    public static void LoadBindings(InputActionAsset asset)
    {
        if (asset == null) return;
        if (PlayerPrefs.HasKey("rebinds"))
        {
            string rebinds = PlayerPrefs.GetString("rebinds");
            asset.LoadBindingOverridesFromJson(rebinds);
            Debug.Log("[KeybindManager] Bindings loaded from PlayerPrefs.");
        }
    }

    /// <summary>
    /// Removes all custom bindings and restores defaults.
    /// </summary>
    public static void ResetBindings(InputActionAsset asset)
    {
        if (asset == null) return;
        asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey("rebinds");
        PlayerPrefs.Save();
        Debug.Log("[KeybindManager] Bindings reset to defaults.");
    }
}
