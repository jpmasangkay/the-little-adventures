using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindButton : MonoBehaviour
{
    public InputActionReference actionReference;
    public int bindingIndex;
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI bindText;
    public Button rebindButton;

    private void OnEnable()
    {
        if (actionReference != null && actionReference.action != null)
        {
            if (actionNameText != null) actionNameText.text = actionReference.action.name;
            
            // Get initial readable string
            if (bindText != null)
            {
                bindText.text = InputControlPath.ToHumanReadableString(
                    actionReference.action.bindings[bindingIndex].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
        }
    }

    private void Start()
    {
        if (rebindButton != null)
        {
            rebindButton.onClick.AddListener(() =>
            {
                if (KeybindManager.Instance != null && actionReference != null)
                {
                    KeybindManager.Instance.StartRebinding(actionReference.action, bindingIndex, bindText);
                }
                else
                {
                    Debug.LogError("[KeybindButton] Missing KeybindManager in the scene!");
                }
            });
        }
    }
}
