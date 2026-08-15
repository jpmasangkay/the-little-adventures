using UnityEngine;

/// <summary>
/// Place this on your main UI Canvas Prefab (that holds Pause, Settings, Keybinds, Inventory).
/// It ensures there is exactly ONE active UI in the game, preventing duplicates or missing UIs in other scenes.
/// </summary>
public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        var evt = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (evt != null)
        {
            DontDestroyOnLoad(evt.gameObject);
        }
        else
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // Add the new input system UI module
            var uiModule = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            
            DontDestroyOnLoad(esObj);
            Debug.Log("[GlobalUIManager] Created and persisted a new EventSystem.");
        }
    }
}
