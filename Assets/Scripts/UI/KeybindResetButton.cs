using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeybindResetButton : MonoBehaviour
{
    public Button resetButton;
    public InputActionAsset inputActions;

    private void Start()
    {
        if (resetButton != null && inputActions != null)
        {
            resetButton.onClick.AddListener(() =>
            {
                KeybindManager.ResetBindings(inputActions);
                
                // Force a reload of the scene so UI buttons refresh their text
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
        }
    }
}
